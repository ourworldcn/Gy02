using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Conditional;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using System;
using System.Collections;
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
            if (condition.TId.HasValue && condition.TId.Value != entity.TemplateId) return false; //若模板id不匹配
            else if (condition.MinCount > entity.Count) return false;   //若低于要求值
            else if (condition.MaxCount < entity.Count) return false;   //若高于要求值

            var thing = entity.GetThing();
            var fullView = _TemplateManager.GetFullViewFromId(entity.TemplateId);

            if (condition.Genus?.Count > 0)    //若需要限制类属
                if (fullView.Genus is null || condition.Genus.Intersect(fullView.Genus).Count() != condition.Genus.Count)
                    return false;
            if (condition.ParentTId.HasValue)
                if (thing.Parent is null || condition.ParentTId.Value != thing.Parent.ExtraGuid)
                    return false;
            if (!condition.GeneralConditional.All(c =>
            {
                if (!TryGetValueFromConditionalItem(c, out var obj, entity))
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
            return !coll.Any() || coll.Any(c => IsMatch(entity, c));
        }

        /// <summary>
        /// 计算扩展函数。
        /// </summary>
        /// <param name="conditionalItem"></param>
        /// <param name="value"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public bool TryGetValueFromConditionalItem(GeneralConditionalItem conditionalItem, out object value, params object[] objects)
        {
            var result = false;
            switch (conditionalItem.Operator)
            {
                case "ToInt32":
                    {
                        var pName = conditionalItem.Args[0];
                        var obj = objects[0];
                        var pi = obj.GetType().GetProperty(pName);
                        if (pi is null)
                        {
                            OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定属性，属性名={pName}");
                            value = default;
                            break;
                        }
                        var tmp = pi.GetValue(obj);
                        value = Convert.ToInt32(tmp);
                        result = true;
                    }
                    break;
                case "GetBuyedCount":
                    {
                        var now = OwHelper.WorldNow;
                        if (objects[0] is not GameChar gameChar)
                        {
                            value = 0; break;
                        }
                        if (!Guid.TryParse(conditionalItem.Args[0].ToString(), out var tid)) //商品的TId
                        {
                            value = 0; break;
                        }
                        if (objects.Length > 1 && objects[1] is GameThingPreconditionItem gtpi)  //若有父对象
                        {

                        }
                        var tt = _TemplateManager.GetFullViewFromId(tid);
                        var list = gameChar.ShoppingHistoryV2;
                        if (IsExistsPeriod(tt.ShoppingItem.Ins)) //若存在自转周期
                        {
                            var period = GetPeriodIndex(tt.ShoppingItem.Ins, gameChar, out _);
                            var val = list.Where(c => c.TId == tid && c.PeriodIndex == period).Sum(c => c.Count);  //如果source不包含任何元素，则Sum(IEnumerable<Decimal>)方法返回零。
                            value = Convert.ToInt32(val);
                            result = true;
                        }
                        else
                        {
                            if (!tt.ShoppingItem.Period.IsValid(now, out var start))
                            {
                                value = 0; break;
                            }
                            var val = list.Where(c => c.TId == tid && c.WorldDateTime >= start && c.WorldDateTime <= now).Sum(c => c.Count);  //如果source不包含任何元素，则Sum(IEnumerable<Decimal>)方法返回零。
                            value = Convert.ToInt32(val);
                            result = true;
                        }
                    }
                    break;
                case "ModE":
                    {
                        //获取属性值
                        var pName = conditionalItem.PropertyName;
                        var obj = objects[0];
                        var pi = obj.GetType().GetProperty(pName);
                        if (pi is null)
                        {
                            OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定属性，属性名={pName}");
                            value = default;
                            break;
                        }
                        var tmp = pi.GetValue(obj);
                        var val = Convert.ToDecimal(tmp);
                        if (!OwConvert.TryToDecimal(conditionalItem.Args[0], out var arg0) || !OwConvert.TryToDecimal(conditionalItem.Args[1], out var arg1))
                        {
                            OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                            OwHelper.SetLastErrorMessage($"ModE要有两个参数都是数值型。");
                            result = false;
                            value = default;
                            break;
                        }
                        else
                            OwHelper.SetLastError(ErrorCodes.NO_ERROR);
                        value = val % arg0 == arg1;
                        result = true;
                    }
                    break;
                default:
                    value = default;
                    break;
            }
            return result;
        }

        #endregion 通用条件判断方法

        #region 周期相关

        /// <summary>
        /// 获取周期号。
        /// </summary>
        /// <param name="inItems"></param>
        /// <param name="gameChar"></param>
        /// <param name="entity"></param>
        /// <returns>不是空则获取到了周期数。否则是无效周期 -或- 指定的输入项接中没有自周期限定条件。</returns>
        public int? GetPeriodIndex(IEnumerable<BlueprintInItem> inItems, GameChar gameChar, out GameEntity entity)
        {
            var item1 = inItems?.FirstOrDefault(inItem => inItem.Conditional.Any(c => c.NumberCondition is not null));
            if (item1 is null)
            {
                entity = null;
                return null;
            }
            var index = GetPeriodIndex(item1, gameChar, out entity);
            return index;
        }

        /// <summary>
        /// 是否存在自转周期。
        /// </summary>
        /// <param name="inItems"></param>
        /// <returns></returns>
        public bool IsExistsPeriod(IEnumerable<BlueprintInItem> inItems)
        {
            var item1 = inItems?.FirstOrDefault(inItem => inItem.Conditional.Any(c => c.NumberCondition is not null));
            if (item1 is null) return false;
            return true;

        }

        /// <summary>
        /// 获取周期号。
        /// </summary>
        /// <param name="inItem"></param>
        /// <param name="gameChar"></param>
        /// <param name="entity"></param>
        /// <returns>不是空则获取到了周期号。否则是无效周期。</returns>
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
                //if (c.TemplateId == Guid.Parse("46542DE4-B8B8-4735-936C-856273B650F7"))
                //    ;
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
            return result;
        }
        #endregion 蓝图输入项相关
    }
}
