using Gy02.Publisher;
using Gy02Bll.Templates;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
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
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class ModifyThingCountCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 修改数量属性。Item2是数量的增量。可能是负值表示减少，正值表示增加。
        /// </summary>
        public List<(VirtualThing, decimal)> Items { get; set; } = new List<(VirtualThing, decimal)>();
    }

    public class ModifyThingCountHandler : SyncCommandHandlerBase<ModifyThingCountCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="templateManager"></param>
        public ModifyThingCountHandler(TemplateManager templateManager)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;

        public override void Handle(ModifyThingCountCommand command)
        {
            var coll = command.Items.Select(c =>
            {
                _TemplateManager.GetEntityAndTemplate(c.Item1, out var entity, out var fullView);
                return (c.Item1, c.Item2, entity, fullView);
            });
            if (!Verify(command))
                return;
            foreach (var item in coll)
                Modify(item.entity as GameEntity, item.Item2, item.fullView, command.Changes);
        }

        bool Verify(ModifyThingCountCommand command)
        {
            var coll = command.Items.Select(c => (c.Item1, c.Item2, _TemplateManager.GetEntityBase(c.Item1) as GameEntity));
            foreach (var item in coll)
            {
                if (item.Item3 is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage("至少有一个对象不是一个可以改变数量的对象。");
                    return false;
                }
                if (!Verify(item.Item3, item.Item2, _TemplateManager.Id2FullView.GetValueOrDefault(item.Item1.ExtraGuid))/*TODO 性能*/)
                {
                    command.FillErrorFromWorld();
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 校验指定物品是否可以执行指定增量。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="count"></param>
        /// <param name="template">物品的模板。</param>
        /// <returns></returns>
        public static bool Verify(GameEntity entity, decimal count, TemplateStringFullView template)
        {
            Debug.Assert(template.Stk >= -1);
            Debug.Assert(template.TemplateId == entity.TemplateId);
            var entityType = TemplateManager.GetTypeFromTemplate(template);

            if (entity.Count + count < 0)  //数量过小
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                OwHelper.SetLastErrorMessage($"{template.DisplayName}({entity.Id})修改后数量为负值。");
                return false;
            }
            if (template.Stk == -1) //若无限堆叠
                return true;
            if (entity.Count + count > template.Stk)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                OwHelper.SetLastErrorMessage($"{template.DisplayName}({entity.Id})修改后数量超过堆叠上限。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 修改虚拟物的数量。不进行参数校验的修改数量属性。并根据需要返回更改数据。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="count">数量的增量。</param>
        /// <param name="template"></param>
        /// <param name="changes"></param>
        static void Modify(GameEntity entity, decimal count, TemplateStringFullView template, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var oldCount = entity.Count;
            entity.Count += count;
            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = entity.Thing,
                HasNewValue = true,
                HasOldValue = true,
                OldValue = oldCount,
                NewValue = entity.Count,
                PropertyName = "Count",
            });
        }

    }
}
