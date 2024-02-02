using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameSearcherManagerOptions : IOptions<GameSearcherManagerOptions>
    {
        public GameSearcherManagerOptions()
        {
        }

        public GameSearcherManagerOptions Value => this;
    }

    /// <summary>
    /// 搜索匹配相关功能管理器。内部函数不考虑转换TId的问题——都需要是最终条件项。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameSearcherManager : GameManagerBase<GameSearcherManagerOptions, GameSearcherManager>
    {

        public GameSearcherManager(IOptions<GameSearcherManagerOptions> options, ILogger<GameSearcherManager> logger,
            GameTemplateManager templateManager, GameEntityManager entityManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
        }

        private GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;

        #region 通用条件判断方法

        /// <summary>
        /// 指定实体是否符合指定条件的要求。此函数不考虑条件组掩码问题。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition">最终条件，指定的所有模板id必须已被翻译。</param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, GameThingPreconditionItem condition)
        {
            if (condition.TId.HasValue) //若需要考虑模板id
                if (condition.TId.Value != entity.TemplateId)
                    return false;
            if (condition.MinCount > entity.Count) return false;
            if (condition.MaxCount < entity.Count) return false;
            VirtualThing thing = entity.GetThing();
            TemplateStringFullView fullView = _TemplateManager.GetFullViewFromId(thing.ExtraGuid);

            if (condition.Genus is not null && condition.Genus.Count > 0)    //若需要限制类属
                if (fullView.Genus is null || condition.Genus.Intersect(fullView.Genus).Count() != condition.Genus.Count)
                    return false;
            if (condition.ParentTId.HasValue)
                if (thing.Parent is null || condition.ParentTId.Value != thing.Parent.ExtraGuid)
                    return false;
            if (!condition.GeneralConditional.All(c =>
            {
                if (!_TemplateManager.TryGetValueFromConditionalItem(c, out var obj, entity))
                    return false;
                if (!OwConvert.TryGetBoolean(obj, out var result))
                    return false;
                return result;
            }))  //若通用属性要求的条件不满足
                return false;
            if (condition.NumberCondition is NumberCondition nc) //若需要判断数值条件
            {
                if (entity.GetType().GetProperty(nc.PropertyName) is not PropertyInfo pi || !OwConvert.TryToDecimal(pi.GetValue(entity), out var deci)) return false; //若非数值属性
                if (!nc.IsMatch(deci)) return false;
            }
            return true;
        }

        /// <summary>
        /// 在指定的实体集合中找到第一个匹配的项。此函数不考虑条件组掩码问题。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="condition">最终条件，指定的所有模板id必须已被翻译。</param>
        /// <param name="entity">如果返回true这里给出匹配的项。</param>
        /// <returns>true找到一个项，否则返回false。</returns>
        public bool GetFirstMatch(IEnumerable<GameEntity> entities, GameThingPreconditionItem condition, out GameEntity entity)
        {
            entity = entities.FirstOrDefault(c => IsMatch(c, condition));
            return entity != null;
        }

        /// <summary>
        /// 获取一个指示，指定实体是否满足一组条件的要求。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="entity"></param>
        /// <param name="conditions">其中条件必须已经被转换为最终条件，不能仍含有序列，随机池等条件。</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, IEnumerable<GameThingPreconditionItem> conditions, int mask)
        {
            var coll = conditions.Where(c => c.IsValidate(mask));
            return coll.All(c => IsMatch(entity, c));
        }

        #endregion 通用条件判断方法

        #region 周期相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inItems"></param>
        /// <param name="gameChar"></param>
        /// <param name="entity"></param>
        /// <returns>不是空则获取到了周期数。否则是无效周期 -或- 指定的输入项接中没有自周期限定条件。</returns>
        public int? GetPeriodIndex(IEnumerable<BlueprintInItem> inItems, GameChar gameChar, out GameEntity entity)
        {
            var item1 = inItems?.FirstOrDefault(inItem => inItem.Conditional.Any(c => c.NumberCondition is NumberCondition));
            if (item1 is null)
            {
                entity = null;
                return null;
            }
            var index = GetPeriodIndex(item1, gameChar, out entity);
            return index;
        }

        /// <summary>
        /// 获取周期数。
        /// </summary>
        /// <param name="inItem"></param>
        /// <param name="gameChar"></param>
        /// <param name="entity"></param>
        /// <returns>不是空则获取到了周期数。否则是无效周期。</returns>
        public int? GetPeriodIndex(BlueprintInItem inItem, GameChar gameChar, out GameEntity entity)
        {
            var allEntity = _EntityManager.GetAllEntity(gameChar);
            var gtc = inItem.Conditional.FirstOrDefault(c => c.NumberCondition is not null);

            if (!GetFirstMatch(allEntity, gtc, out entity)) goto lbEmpty;

            var number = gtc.NumberCondition.GetNumber(entity);
            if (number is null) goto lbEmpty;

            var period = gtc.NumberCondition.GetPeriodIndex(number.Value);

            return period;
        lbEmpty:
            entity = null;
            return null;
        }

        #endregion 周期相关

        #region 蓝图输入项相关
        /// <summary>
        /// 按条件掩码确定是否匹配。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="inItem">必须是最终条件，不考虑转换问题。</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, BlueprintInItem inItem, int mask)
        {
            if (!IsMatch(entity, inItem.Conditional, mask)) return false;
            if (inItem.Count > entity.Count && inItem.Conditional.Any(c => c.IsValidate(mask))) return false;
            return true;
        }

        /// <summary>
        /// 获取第一个匹配项。
        /// 不考虑转换等因素。
        /// </summary>
        /// <param name="mng">蓝图管理器。</param>
        /// <param name="entities"></param>
        /// <param name="inItem"></param>
        /// <param name="mask">条件组掩码</param>
        /// <returns>返回符合条件的实体，null表示没有找到合适的实体。</returns>
        public bool GetMatch(IEnumerable<GameEntity> entities, BlueprintInItem inItem, int mask, out GameEntity entity)
        {
            entity = entities.FirstOrDefault(c =>
            {
                return IsMatch(c, inItem, mask);
            });
            return entity != null;
        }

        /// <summary>
        /// 获取每个条件匹配的项。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="inItems">必须都是最终条件。</param>
        /// <param name="mask">只考虑符合该掩码的条件。</param>
        /// <returns>针对每一个输入项有一个匹配项。如果某个项是null则说明没有找到匹配项。</returns>
        public IEnumerable<(GameEntity, BlueprintInItem)> GetMatches(IEnumerable<GameEntity> entities, IEnumerable<BlueprintInItem> inItems, int mask)
        {
            var result = new List<(GameEntity, BlueprintInItem)>();
            var hs = new HashSet<GameEntity>(entities);
            foreach (var item in inItems.TryToCollection())
            {
                if (GetMatch(hs, item, mask, out var tmp)) hs.Remove(tmp);    //若移除对应的项
                result.Add((tmp, item));
            }
            var tmp1 = hs.FirstOrDefault(c => c.TemplateId == Guid.Parse("46542de4-b8b8-4735-936c-856273b650f7"));
            return result;
        }
        #endregion 蓝图输入项相关
    }
}
