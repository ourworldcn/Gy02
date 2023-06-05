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
    public class BlueprintOptions : IOptions<BlueprintOptions>
    {
        public BlueprintOptions()
        {
        }

        public BlueprintOptions Value => this;
    }

    /// <summary>
    /// 蓝图相关功能管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class BlueprintManager : GameManagerBase<BlueprintOptions, BlueprintManager>
    {
        public BlueprintManager(IOptions<BlueprintOptions> options, ILogger<BlueprintManager> logger, TemplateManager templateManager, GameEntityManager entityManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
        }

        TemplateManager _TemplateManager;
        GameEntityManager _EntityManager;

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
            return _EntityManager.IsMatch(entity, inItem.Conditional, ignore);
        }

        #endregion 计算匹配

        /// <summary>
        /// 在一组实体中选择条件指定实体。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="conditionals"></param>
        /// <returns>返回条件与实体配对的值元组的集合。如果出现任何错误将返回null，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public IEnumerable<(BlueprintInItem, GameEntity)> GetCost(IEnumerable<GameEntity> entities, IEnumerable<BlueprintInItem> conditionals)
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
                result.Add((conditional, entity));
                all.Remove(entity); //去掉已经被匹配的项
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="conditionals"></param>
        /// <param name="changes"></param>
        /// <returns>true成功消耗了所有指定材料，false至少有一种材料不满足条件。</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool Deplete(IEnumerable<GameEntity> entities, IEnumerable<BlueprintInItem> conditionals, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var coll = GetCost(entities, conditionals);
            if (coll is null) return false;
            var tmp = coll.Select(c => (Entity: c.Item2, Count: -Math.Abs(c.Item1.Count))).Where(c => c.Count != 0);
            foreach (var item in tmp.ToArray())
            {
                if (!_EntityManager.Modify(item.Entity, item.Count, changes)) throw new InvalidOperationException { };  //若出现无法减少的异常
            }
            return true;
        }
    }
}
