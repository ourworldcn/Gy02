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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameBlueprintExOptions : IOptions<GameBlueprintExOptions>
    {
        public GameBlueprintExOptions()
        {

        }

        public GameBlueprintExOptions Value => this;
    }

    /// <summary>
    /// 蓝图相关功能管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameBlueprintManagerEx : GameManagerBase<GameBlueprintExOptions, GameBlueprintManager>
    {
        public GameBlueprintManagerEx(IOptions<GameBlueprintExOptions> options, ILogger<GameBlueprintManager> logger, GameContextManager contextManager, GameTemplateManager templateManager) : base(options, logger)
        {
            _ContextManager = contextManager;
            _TemplateManager = templateManager;
        }

        GameContextManager _ContextManager;
        public GameContextManager ContextManager => _ContextManager;


        GameTemplateManager _TemplateManager;
        public GameTemplateManager TemplateManager => _TemplateManager;

        /// <summary>
        /// 指定实体是否符合指定条件的要求。此函数不考虑条件组掩码问题，
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition">最终条件，指定的所有模板id必须已被翻译。</param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, GameThingPreconditionItem condition)
        {
            if (condition.TId.HasValue) //若需要考虑模板id
                if (condition.TId.Value != entity.TemplateId)
                    return false;
            if (condition.MinCount.Value > entity.Count) return false;

            VirtualThing thing = entity.GetThing();
            TemplateStringFullView fullView = _TemplateManager.Id2FullView[thing.ExtraGuid];

            if (condition.Genus is not null && condition.Genus.Count > 0 && (fullView.Genus is null || condition.Genus.Intersect(fullView.Genus).Count() != condition.Genus.Count))
                return false;
            if (condition.ParentTId.HasValue && condition.ParentTId.Value != thing.Parent?.ExtraGuid)
                return false;
            if (condition.NumberCondition is NumberCondition nc) //若需要判断数值条件
            {
                if (entity.GetType().GetProperty(nc.PropertyName) is not PropertyInfo pi || !OwConvert.TryToDecimal(pi.GetValue(entity), out var deci)) return false; //若非数值属性
                if (deci > nc.MaxValue || deci < nc.MinValue) return false;
                var tmp = (deci - nc.Subtrahend) % nc.Modulus;  //余数
                if (tmp < nc.MinRemainder || tmp > nc.MaxRemainder) return false;
            }
            if (!condition.GeneralConditional.All(c =>
            {
                if (!_TemplateManager.TryGetValueFromConditionalItem(c, out var obj, entity))
                    return false;
                if (!OwConvert.TryGetBoolean(obj, out var result))
                    return false;
                return result;
            }))  //若通用属性要求的条件不满足
                return false;
            return true;
        }

    }

    public static class GameBlueprintManagerExExtensions
    {

        public static bool IsMatch(this GameBlueprintManagerEx mng, GameEntity entity, GameThingPreconditionItem conditions, int mask)
        {
            return false;
        }

        /// <summary>
        /// 指定实体是否符合指定条件的要求。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="conditions">为空集合或符合掩码要求的组是空集合，则立即返回true。</param>
        /// <param name="mask">条件掩码，符合掩码要求的条件项才被考虑在内。</param>
        /// <returns></returns>
        public static bool IsMatch(this GameBlueprintManagerEx mng, GameEntity entity, IEnumerable<GameThingPreconditionItem> conditions, int mask)
        {
            var coll = conditions.Where(c => c.IsValidate(mask));
            return coll.All(c => mng.IsMatch(entity, c));
        }
    }
}
