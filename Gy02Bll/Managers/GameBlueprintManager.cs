using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using System.Buffers;

namespace GY02.Managers
{
    public class GameBlueprintOptions : IOptions<GameBlueprintOptions>
    {
        public GameBlueprintOptions()
        {
        }

        public GameBlueprintOptions Value => this;
    }

    /// <summary>
    /// 蓝图相关功能管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameBlueprintManager : GameManagerBase<GameBlueprintOptions, GameBlueprintManager>
    {
        public GameBlueprintManager(IOptions<GameBlueprintOptions> options, ILogger<GameBlueprintManager> logger,
            GameEntityManager entityManager, GameSequenceManager sequenceManager, GameDiceManager diceManager) : base(options, logger)
        {
            _EntityManager = entityManager;
            _SequenceManager = sequenceManager;
            _DiceManager = diceManager;
        }

        GameEntityManager _EntityManager;
        GameSequenceManager _SequenceManager;
        GameDiceManager _DiceManager;

        #region 获取信息


        #endregion 获取信息

        #region 计算匹配

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="entities"></param>
        /// <param name="ignore"></param>
        /// 
        /// <returns></returns>
        public bool IsValid(IEnumerable<BlueprintInItem> ins, IEnumerable<GameEntity> entities, bool ignore = false)
        {
            var result = ins.All(inItem => entities.Any(entity => IsMatch(inItem, entity, ignore)));
            return result;
        }

        /// <summary>
        /// 获取指示，指定的实体是否符合蓝图输入项的要求。
        /// </summary>
        /// <param name="inItem"></param>
        /// <param name="entity"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public bool IsMatch(BlueprintInItem inItem, GameEntity entity, bool ignore = false)
        {
            if (inItem.IgnoreIfDisplayList && ignore) return true;  //若允许忽略
            if (inItem.Count > entity.Count) return false;  //若数量不满足
            foreach (var item in inItem.Conditional)    //遍历检验是否是序列输入
            {
                if (item.TId.HasValue && _SequenceManager.GetTemplateById(item.TId.Value, out var tt)) //若是输入序列
                {
                    var b = _SequenceManager.GetMatches(new GameEntity[] { entity }, tt);
                    if (b.Any()) return true;
                    //if (!_SequenceManager.GetOut(entity, tt, out var summary)) continue;  //没有匹配序列输出
                    //result = entities.FirstOrDefault(c => c.TemplateId == summary.TId && (!summary.ParentTId.HasValue || c.GetThing().Parent.ExtraGuid == summary.ParentTId.Value) && c.Count >= summary.Count);
                    //if (result is not null) return result;
                }
            }
            return _EntityManager.IsMatch(entity, inItem.Conditional, ignore);
        }

        /// <summary>
        /// 获取序列指定的匹配项。
        /// </summary>
        /// <param name="input"></param>
        /// <param name="entities"></param>
        /// <returns>没找到匹配项，则返回空集合。</returns>
        public IEnumerable<(GameEntity, decimal)> GetMatches(BlueprintInItem input, IEnumerable<GameEntity> entities)
        {
            var result = new List<(GameEntity, decimal)> { };
            foreach (var item in input.Conditional)
            {
                if (item.TId.HasValue && _SequenceManager.GetTemplateById(item.TId.Value, out var tt)) //若是序列输入
                {
                    var matches = _SequenceManager.GetMatches(entities, tt);
                    var match = matches.FirstOrDefault();
                    if (match.Item2 is not null)  //若找到匹配项
                    {
                        result.Add((match.Item2, -Math.Abs(match.Item1.Count)));
                        return result;
                    }
                }
            }
            return GetInputs(new BlueprintInItem[] { input }, entities).Where(c => c.Item1 == input).Select(c => (c.Item2, -Math.Abs(c.Item1.Count)));
        }

        /// <summary>
        /// 按条件掩码确定是否匹配。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="inItem"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, BlueprintInItem inItem, int mask)
        {
            if (_EntityManager.IsMatch(entity, inItem.Conditional, mask)) return false;
            if (inItem.Count > entity.Count) return false;
            return true;
        }

        /// <summary>
        /// 翻译需求序列为普通条件。
        /// </summary>
        /// <param name="inItem"></param>
        /// <param name="entities"></param>
        /// <returns>若无需转换则返回<paramref name="inItem"/>，否则返回新实例。</returns>
        public BlueprintInItem Translation(BlueprintInItem inItem, IEnumerable<GameEntity> entities)
        {
            bool changed = false;
            int index = 0;
            var buff = ArrayPool<GameThingPreconditionItem>.Shared.Rent(inItem.Conditional.Count);
            using var dw = DisposeHelper.Create(c => ArrayPool<GameThingPreconditionItem>.Shared.Return(c), buff);
            foreach (var cond in inItem.Conditional)
            {
                var tmp = Translation(cond, entities);
                buff[index++] = tmp;
                changed = changed || !ReferenceEquals(tmp, cond);
            }
            if (changed)    //若发生了变化
            {
                var result = new BlueprintInItem
                {
                    Conditional = buff.Take(inItem.Conditional.Count).ToList(),
                };
                result.Count = result.Conditional.Max(c => c.MinCount ?? 0);
                return result;
            }
            else //若未发生变化
                return inItem;
        }

        /// <summary>
        /// 翻译需求序列为普通条件。
        /// </summary>
        /// <param name="conditional"></param>
        /// <param name="entities"></param>
        /// <return>若无需转换则返回<paramref name="conditional"/>,否则返回转换后的实例。</return>
        public GameThingPreconditionItem Translation(GameThingPreconditionItem conditional, IEnumerable<GameEntity> entities)
        {
            if (!IsNeedTranslation(conditional, out var tt)) return conditional;
            if (!_SequenceManager.GetOut(entities, tt, out var summary)) return null;
            var result = new GameThingPreconditionItem
            {
                TId = summary.TId,
                ParentTId = summary.ParentTId,
                MinCount = summary.Count,
            };
            return result;
        }

        /// <summary>
        /// 是否需要转换。
        /// </summary>
        /// <param name="conditional"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public bool IsNeedTranslation(GameThingPreconditionItem conditional, out TemplateStringFullView template)
        {
            if (conditional.TId.HasValue)
            {
                return _SequenceManager.GetTemplateById(conditional.TId.Value, out template);
            }
            else
            {
                template = null;
                return false;
            }
        }
        #endregion 计算匹配

        /// <summary>
        /// 在一组实体中选择条件指定实体。
        /// </summary>
        /// <param name="conditionals"></param>
        /// <param name="entities"></param>
        /// <returns>返回条件与实体配对的值元组的集合。如果出现任何错误将返回null，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。
        /// 只会返回有消耗的需求的实体，不需要消耗的实体不会返回。</returns>
        public IEnumerable<(BlueprintInItem, GameEntity)> GetInputs(IEnumerable<BlueprintInItem> conditionals, IEnumerable<GameEntity> entities)
        {
            var result = new List<(BlueprintInItem, GameEntity)>();
            var list = new List<BlueprintInItem>(conditionals);
            var all = new HashSet<GameEntity>(entities);
            foreach (var conditional in conditionals)
            {
                var entity = all.Where(c => IsMatch(conditional, c)).FirstOrDefault();
                if (entity is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"无法找到符合条件{conditional}的实体。");
                    return null;
                }
                result.Add((conditional, entity));
                if (conditional.Count == decimal.Zero) continue;    //若此项不消耗
                all.Remove(entity); //去掉已经被匹配的项
            }
            return result;
        }

        /// <summary>
        /// 消耗指定材料。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="conditionals"></param>
        /// <param name="changes"></param>
        /// <returns>true成功消耗了所有指定材料，false至少有一种材料不满足条件。</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool Deplete(IEnumerable<GameEntity> entities, IEnumerable<BlueprintInItem> conditionals, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            List<(GameEntity, decimal)> list = new List<(GameEntity, decimal)>();
            foreach (var conditional in conditionals)
            {
                var ec = GetMatches(conditional, entities);
                if (ec is not IEnumerable<(GameEntity, decimal)> r || !r.Any()) return false;
                list.AddRange(ec);
            }
            var tmp = list.Select(c => (Entity: c.Item1, Count: -Math.Abs(c.Item2))).Where(c => c.Count != 0);
            return _EntityManager.Modify(tmp, changes);
        }

    }

    public static class GameBlueprintManagerExtensions
    {
        /// <summary>
        /// 获取第一个匹配项。
        /// 不考虑转换等因素。
        /// </summary>
        /// <param name="mng">蓝图管理器。</param>
        /// <param name="entities"></param>
        /// <param name="inItem"></param>
        /// <param name="mask">条件组掩码</param>
        /// <returns>返回符合条件的实体，null表示没有找到合适的实体。</returns>
        public static GameEntity GetMatch(this GameBlueprintManager mng, IEnumerable<GameEntity> entities, BlueprintInItem inItem, int mask)
        {
            var result = entities.FirstOrDefault(c =>
            {
                return mng.IsMatch(c, inItem, mask);
            });
            return result;
        }


    }
}
