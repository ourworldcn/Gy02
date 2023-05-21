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

        public void GetCost(TemplateStringFullView fullView, GameEntity mainItem, List<GameEntity> items, GameChar gc, ICollection<GamePropertyChangeItem<object>> changes)
        {
            foreach (var item in fullView.In)
            {
                _EntityManager.IsMatch(mainItem, item.Conditional);
            }
        }

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
                var entity = all.Where(c => IsMatch(c, conditional)).FirstOrDefault();
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

        /// <summary>
        /// 按照指定的输出项生成实体。
        /// </summary>
        /// <param name="outItems"></param>
        /// <returns></returns>
        public IEnumerable<GameEntity> GenerateOuts(IEnumerable<BlueprintOutItem> outItems)
        {
            var result = _EntityManager.Create(outItems.Select(c => (c.TId, c.Count)));
            if (result is null) return null;
            return result;
        }

        /// <summary>
        /// 获取指示，指定的实体是否符合蓝图输入项的要求。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="inItem"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity item, BlueprintInItem inItem)
        {
            if (item.Count < inItem.Count)
                return false;
            return _EntityManager.IsMatch(item, inItem.Conditional);
        }

        /// <summary>
        /// 获取指示，指定的实体是否符合蓝图输入项中的一项。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="inItems"></param>
        /// <param name="result"></param>
        /// <returns>true找到了输入项。</returns>
        public bool IsMatch(GameEntity entity, IEnumerable<BlueprintInItem> inItems, out BlueprintInItem result)
        {
            result = inItems.FirstOrDefault(c => IsMatch(entity, c));
            return result is not null;
        }

        /// <summary>
        /// 获取材料的匹配关系。
        /// </summary>
        /// <remarks>会避免重复获取同一个材料多次，但无法达到最佳匹配。</remarks>
        /// <param name="inItems"></param>
        /// <param name="entities"></param>
        /// <returns></returns>
        public IEnumerable<(BlueprintInItem, GameEntity)> Matches(IEnumerable<BlueprintInItem> inItems, IEnumerable<GameEntity> entities)
        {
            HashSet<GameEntity> hsEntities = new HashSet<GameEntity>(entities);
            List<(BlueprintInItem, GameEntity)> result = new List<(BlueprintInItem, GameEntity)>();
            foreach (var item in inItems)
            {
                var entity = hsEntities.FirstOrDefault(c => IsMatch(c, item));
                if (entity is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"找不到{item}要求的材料。");
                    return null;
                }
                var b = hsEntities.Remove(entity);
                Debug.Assert(b);
                result.Add((item, entity));
            }
            //TODO 未对多重匹配的问题做处理
            return result;
        }
    }
}
