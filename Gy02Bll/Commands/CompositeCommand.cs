using Gy02.Publisher;
using Gy02Bll.Managers;
using Gy02Bll.Templates;
using Microsoft.IdentityModel.Tokens;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class CompositeCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CompositeCommand() { }

        /// <summary>
        /// 指定的蓝图。
        /// </summary>
        public TemplateStringFullView Blueprint { get; set; }

        /// <summary>
        /// 指定主要材料。
        /// </summary>
        public GameEntity MainItem { get; set; }

        /// <summary>
        /// 指定使用的辅助材料。
        /// </summary>
        public List<GameEntity> Items { get; set; } = new List<GameEntity>();

        /// <summary>
        /// 针对的角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }
    }

    public class CompositeHandler : SyncCommandHandlerBase<CompositeCommand>
    {
        public CompositeHandler(TemplateManager templateManager, GameAccountStore gameAccountStore, BlueprintManager blueprintManager, GameEntityManager gameEntityManager, SyncCommandManager syncCommandManager)
        {
            _TemplateManager = templateManager;
            _GameAccountStore = gameAccountStore;
            _BlueprintManager = blueprintManager;
            _GameEntityManager = gameEntityManager;
            _SyncCommandManager = syncCommandManager;
        }

        TemplateManager _TemplateManager;
        GameAccountStore _GameAccountStore;
        BlueprintManager _BlueprintManager;
        GameEntityManager _GameEntityManager;
        SyncCommandManager _SyncCommandManager;

        public override void Handle(CompositeCommand command)
        {
            using var dw = DisposeHelper.Create(_GameAccountStore.Lock, _GameAccountStore.Unlock, command.GameChar.GetUser().GetKey(), TimeSpan.FromSeconds(2));
            if (dw.IsEmpty)
            {
                command.FillErrorFromWorld();
                return;
            }
            var bp = command.Blueprint;
            if ((bp?.In?.Count ?? 0) <= 0 || (bp?.Out?.Count ?? 0) <= 0)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "指定蓝图Id不正确。";
                return;

            }
            if (!bp.In.Any(c => _BlueprintManager.IsMatch(command.MainItem, c)))
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "主材料不符合蓝图输入项的要求。";
                return;
            }
            if (!command.Items.All(item => bp.In.Any(c => _BlueprintManager.IsMatch(item, c))))
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "至少一个材料不符合蓝图输入项的要求。";
                return;
            }

            var cost = command.Items.Append(command.MainItem).Select(c => (c, -c.Count));
            var modifyCount = new ModifyEntityCountCommand { Items = cost.ToList() };
            _SyncCommandManager.Handle(modifyCount);
            if (modifyCount.HasError)
            {
                command.FillErrorFrom(modifyCount);
                return;
            }
            command.Changes?.AddRange(modifyCount.Changes);

            List<GameEntity> entities = new List<GameEntity>();
            foreach (var item in bp.Out)    //生成输出项
            {
                var createThing = new CreateVirtualThingCommand { TemplateId = item.TId };
                _SyncCommandManager.Handle(createThing);
                if (createThing.HasError)
                {
                    command.FillErrorFrom(createThing);
                    return;
                }
                entities.Add((GameEntity)_TemplateManager.GetEntityBase(createThing.Result, out _));
            }
            var move = new MoveEntitiesCommand { Items = entities };
            _SyncCommandManager.Handle(move);
            if (move.HasError) { command.FillErrorFrom(move); return; }
            command?.Changes.AddRange(move.Changes);
            //生成合成材料记录
            var mainOut = entities.First(); //主要输出物
            var oldCost = mainOut.CompositingAccruedCost?.ToArray() ?? Array.Empty<GameEntitySummary>();    //旧的合成材料记录
            var newCost = oldCost.Concat(cost.Select(c => new GameEntitySummary { Count = c.Item2, TId = c.c.TemplateId })).ToArray();   //新材料记录
            command.Changes?.Add(new GamePropertyChangeItem<object>
            {
                Object = mainOut,
                PropertyName = nameof(mainOut.CompositingAccruedCost),

                HasOldValue = oldCost.Length > 0,
                OldValue = oldCost,

                HasNewValue = newCost.Length > 0,
                NewValue = newCost,
            });
            mainOut.CompositingAccruedCost.Clear();
            mainOut.CompositingAccruedCost.AddRange(newCost);
        }
    }
}
