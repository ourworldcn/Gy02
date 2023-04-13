using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Combat
{
    public class CombatMarkCommand : SyncCommandBase
    {
        public CombatMarkCommand() { }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要标记的战斗信息。
        /// </summary>
        public string CombatInfo { get; set; }
    }

    public class CombatMarkHandler : SyncCommandHandlerBase<CombatMarkCommand>
    {
        public CombatMarkHandler()
        {

        }

        public override void Handle(CombatMarkCommand command)
        {
            command.GameChar.ClientCombatInfo = command.CombatInfo;

        }
    }
}
