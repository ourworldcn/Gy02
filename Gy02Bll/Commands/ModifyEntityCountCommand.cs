using Gy02.Publisher;
using Gy02Bll.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public class ModifyEntityCountCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 修改数量属性。Item2是数量的增量。可能是负值表示减少，正值表示增加。
        /// </summary>
        public List<(GameEntity, decimal)> Items { get; set; } = new List<(GameEntity, decimal)>();
    }

    public class ModifyEntityCountHandler : SyncCommandHandlerBase<ModifyEntityCountCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="templateManager"></param>
        public ModifyEntityCountHandler(TemplateManager templateManager)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(ModifyEntityCountCommand command)
        {
            var coll = command.Items.Select(c =>
            {
                var fullView = _TemplateManager.Id2FullView.GetValueOrDefault(c.Item1.TemplateId);
                return (c.Item1, c.Item2, fullView);
            });
            if (!Verify(command))
                return;
            foreach (var item in coll)
                Modify(item.Item1, item.Item2, item.fullView, command.Changes);
        }

        /// <summary>
        /// 校验参数。
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        bool Verify(ModifyEntityCountCommand command)
        {
            foreach (var item in command.Items)
            {
                var tt = _TemplateManager.Id2FullView.GetValueOrDefault(item.Item1.TemplateId);
                if (!Verify(item.Item1, item.Item2, tt))
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
        /// <param name="count">数量的增量，正数表示增加，负数表示失败。</param>
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
        public static void Modify(GameEntity entity, decimal count, TemplateStringFullView template, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var oldCount = entity.Count;
            entity.Count += count;
            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = entity,
                PropertyName = "Count",
                DateTimeUtc = DateTime.UtcNow,
                HasOldValue = true,
                OldValue = oldCount,
                HasNewValue = true,
                NewValue = entity.Count,
            });
            if (entity.Count == 0 && !template.Count0Reserved) //若需要删除
            {
                var thing = entity.Thing as VirtualThing;
                var parent = thing.Parent;
                thing.Parent.Children.Remove(thing); thing.Parent = null; thing.ParentId = null;
                changes?.CollectionRemove(thing, parent);
                var db = (thing.GetRoot() as VirtualThing)?.RuntimeProperties.GetValueOrDefault(nameof(DbContext)) as DbContext;
                db?.Remove(thing);
            }
        }

    }
}
