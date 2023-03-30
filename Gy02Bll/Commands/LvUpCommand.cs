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
    public class LvUpCommand : PropertyChangeCommandBase
    {
        public LvUpCommand() { }

        /// <summary>
        /// 发出此操作的角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    public class LvUpCommandHandler : SyncCommandHandlerBase<LvUpCommand>
    {

        public LvUpCommandHandler(TemplateManager templateManager)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;

        public override void Handle(LvUpCommand command)
        {
            var entities = command.Ids.Join(command.GameChar.GetAllChildren(), c => c, c => c.Id, (l, r) => r).Select(c =>
            {
                if(!_TemplateManager.GetEntityAndTemplate(c,out var entity,out var fv))
                {

                }
                return (entity, fv);
            });
        }
    }
}
