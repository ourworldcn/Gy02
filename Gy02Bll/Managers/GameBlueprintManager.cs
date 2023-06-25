using GY02.Commands;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
        public GameBlueprintManager(IOptions<GameBlueprintOptions> options, ILogger<GameBlueprintManager> logger, GameEntityManager entityManager, GameSequenceManager sequenceManager) : base(options, logger)
        {
            _EntityManager = entityManager;
            _SequenceManager = sequenceManager;
        }

        GameEntityManager _EntityManager;
        GameSequenceManager _SequenceManager;

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
            var result = ins.All(inItem => entities.Any(entity => IsValid(inItem, entity, ignore)));
            return result;
        }

        /// <summary>
        /// 获取指示，指定的实体是否符合蓝图输入项的要求。
        /// </summary>
        /// <param name="inItem"></param>
        /// <param name="entity"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public bool IsValid(BlueprintInItem inItem, GameEntity entity, bool ignore = false)
        {
            if (inItem.IgnoreIfDisplayList && ignore) return true;  //若允许忽略
            if (inItem.Count > entity.Count) return false;  //若数量不满足
            foreach (var item in inItem.Conditional)    //遍历检验是否是序列输入
            {
                if (item.TId.HasValue && _SequenceManager.GetTemplateById(item.TId.Value, out var tt)) //若是输入序列
                {

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
                var entity = all.Where(c => IsValid(conditional, c)).FirstOrDefault();
                if (entity is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"无法找到符合条件{conditional}的实体。");
                    return null;
                }
                if (conditional.Count == decimal.Zero) continue;    //若此项不消耗
                result.Add((conditional, entity));
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
            var coll = GetInputs(conditionals, entities);
            if (coll is null) return false;
            var tmp = coll.Select(c => (Entity: c.Item2, Count: -Math.Abs(c.Item1.Count))).Where(c => c.Count != 0);
            return _EntityManager.Modify(tmp, changes);
        }

    }
}
