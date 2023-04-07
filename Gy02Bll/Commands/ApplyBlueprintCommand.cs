using Gy02.Publisher;
using Gy02Bll.Managers;
using Gy02Bll.Templates;
using Microsoft.IdentityModel.Tokens;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class ApplyBlueprintCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ApplyBlueprintCommand() { }

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

    public class ApplyBlueprintHandler : SyncCommandHandlerBase<ApplyBlueprintCommand>
    {
        public ApplyBlueprintHandler(TemplateManager templateManager, GameAccountStore gameAccountStore)
        {
            _TemplateManager = templateManager;
            _GameAccountStore = gameAccountStore;
        }

        TemplateManager _TemplateManager;
        GameAccountStore _GameAccountStore;

        public override void Handle(ApplyBlueprintCommand command)
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
            //var id = command.Items.FirstOrDefault();
            //var item = gc.GetAllChildren().FirstOrDefault(c => c.Id == id);
            //if (item is null)
            //{
            //    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    command.DebugMessage = "指定物品不正确。";
            //    return;
            //}
            //var entity = _TemplateManager.GetEntityBase(item, out var type) as GameEntity;
            //var costs = _TemplateManager.GetCost(entity, gc.GetAllChildren().Select(c => _TemplateManager.GetEntityBase(c, out _)).OfType<GameEntity>());   //代价
            //var modifyCounCommand = new ModifyThingCountCommand { Changes = command.Changes };
        }
    }
}
