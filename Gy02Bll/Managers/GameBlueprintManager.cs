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
using System.Runtime.CompilerServices;

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
            GameEntityManager entityManager, GameSequenceManager sequenceManager, GameDiceManager diceManager, GameTemplateManager templateManager) : base(options, logger)
        {
            _EntityManager = entityManager;
            _SequenceManager = sequenceManager;
            _DiceManager = diceManager;
            _TemplateManager = templateManager;
        }

        GameEntityManager _EntityManager;
        GameSequenceManager _SequenceManager;
        GameDiceManager _DiceManager;
        GameTemplateManager _TemplateManager;

        #region 获取信息


        #endregion 获取信息

        #region 计算匹配

        /// <summary>
        /// 按条件掩码确定是否匹配。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="inItem">必须是最终条件，不考虑转换问题。</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, BlueprintInItem inItem, int mask)
        {
            if (!_EntityManager.IsMatch(entity, inItem.Conditional, mask)) return false;
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

        #region 计算寻找物品的匹配

        /// <summary>
        /// 指定材料是否符合指定条件的要求。
        /// </summary>
        /// <param name="main">要升级的物品</param>
        /// <param name="cost">要求的条件。</param>
        /// <param name="entity">材料的实体。</param>
        /// <param name="count">实际耗费的数量</param>
        /// <returns>true表示指定实体符合指定条件，否则返回false。</returns>
        public bool IsMatch(GameEntity main, CostInfo cost, GameEntity entity, out decimal count)
        {
            if (_EntityManager.IsMatch(entity, cost.Conditional, 1))
            {
                var lv = Convert.ToInt32(main.Level);
                if (cost.Conditional is not null && cost.Counts.Count > lv)
                {
                    var tmp = cost.Counts[lv];  //耗费的数量
                    if (tmp <= entity.Count)
                    {
                        count = -Math.Abs(tmp);
                        return true;
                    }
                }
            }
            count = 0;
            return false;
        }

        /// <summary>
        /// 获取指定物品的升级所需材料及数量列表。
        /// </summary>
        /// <param name="entity">要升级的物品。</param>
        /// <param name="alls">搜索物品的集合。</param>
        /// <returns>升级所需物及数量列表。返回null表示出错了 <seealso cref="OwHelper.GetLastError"/>。
        /// 如果返回了找到的条目<see cref="ValueTuple{GameEntity,Decimal}.Item2"/> 对于消耗的物品是负数，可能包含0.。</returns>
        public List<(GameEntity, decimal)> GetCost(GameEntity entity, IEnumerable<GameEntity> alls)
        {
            List<(GameEntity, decimal)> result = null;
            var fullView = _TemplateManager.Id2FullView.GetValueOrDefault(entity.TemplateId);

            if (fullView?.LvUpTId is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return result;
            }
            var tt = _TemplateManager.Id2FullView.GetValueOrDefault(fullView.LvUpTId.Value);
            if (tt?.LvUpData is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return result;
            }
            var lv = Convert.ToInt32(entity.Level);
            var coll = tt.LvUpData.Select(c =>
            {
                decimal count = 0;
                var tmp = alls.FirstOrDefault(item => IsMatch(entity, c, item, out count));
                return (entity: tmp, count);
            }).ToArray();
            if (coll.Count(c => c.entity is not null) != tt.LvUpData.Count)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})升级材料不全。");
                return result;
            }
            var errItem = coll.GroupBy(c => c.entity.Id).Where(c => c.Count() > 1).FirstOrDefault();
            if (errItem is not null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"物品(Id={errItem.First().entity.Id})同时符合两个或更多条件。");
                return result;
            }

            return result = coll.ToList();
        }

        #endregion 计算寻找物品的匹配


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
            var collCond = conditionals.Select(c => Translation(c,entities));
            var coll = this.GetMatches(entities, collCond, 1);
            var errItem = coll.FirstOrDefault(c => c.Item1 is null);
            if (errItem.Item2 is not null)   //若存在无法找到的项
            {
                return false;
            }
            var tmp = coll.Select(c => (Entity: c.Item1, Count: -Math.Abs(c.Item2.Count))).Where(c => c.Count != 0);
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

        /// <summary>
        /// 获取每个条件匹配的项。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="entities"></param>
        /// <param name="inItems">必须都是最终条件。</param>
        /// <param name="mask">只考虑符合该掩码的条件。</param>
        /// <returns>针对每一个输入项有一个匹配项。如果某个项是null则说明没有找到匹配项。</returns>
        public static IEnumerable<(GameEntity, BlueprintInItem)> GetMatches(this GameBlueprintManager mng, IEnumerable<GameEntity> entities, IEnumerable<BlueprintInItem> inItems, int mask)
        {
            var result = new List<(GameEntity, BlueprintInItem)>();
            var hs = new HashSet<GameEntity>(entities);
            foreach (var item in inItems.TryToCollection())
            {
                var tmp = mng.GetMatch(hs, item, mask);
                if (tmp is not null) hs.Remove(tmp);    //若移除对应的项
                result.Add((tmp, item));
            }
            return result;
        }

        /// <summary>
        /// 获取一个指示，指定实体是否满足所有指定条件。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="entity"></param>
        /// <param name="inItems">必须都是最终条件。</param>
        /// <param name="mask">只考虑符合该掩码的条件。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(this GameBlueprintManager mng, GameEntity entity, IEnumerable<BlueprintInItem> inItems, int mask)
        {
            return inItems.All(c => mng.IsMatch(entity, c, mask));
        }
    }
}
