using Gy02.Publisher;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
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

        public bool IsMatch(GameEntity item, BlueprintInItem inItem)
        {
            if (item.Count < inItem.Count)
                return false;
            return _EntityManager.IsMatch(item, inItem.Conditional);
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
