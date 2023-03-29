using Gy02.Publisher;
using Gy02Bll.Managers;
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
        public ApplyBlueprintCommand() { }

        public Guid Token { get; set; }

        public Guid BlueprintId { get; set; }

        public List<Guid> Items { get; set; } = new List<Guid>();
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
            using var dw = _GameAccountStore.GetCharFromToken(command.Token, out var gc);
            if (dw.IsEmpty)
            {
                command.FillErrorFromWorld();
                return;
            }
            var bp = _TemplateManager.Id2FullView.GetValueOrDefault(command.BlueprintId);
            if (bp is null || bp.LvUpData is null || bp.LvUpData.Count <= 0)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "指定蓝图Id不正确。";
                return;
            }
            var id = command.Items.FirstOrDefault();
            var item = gc.GetAllChildren().FirstOrDefault(c => c.Id == id);
            if (item is null)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "指定物品不正确。";
                return;
            }
            var entity = _TemplateManager.GetEntityBase(item, out var type) as GameEntity;
            var costs = _TemplateManager.GetCost(entity, gc.GetAllChildren().Select(c => _TemplateManager.GetEntityBase(c, out _)).OfType<GameEntity>());   //代价
            //var modifyCounCommand = new ModifyThingCountCommand { Changes = command.Changes };
        }
    }
}
