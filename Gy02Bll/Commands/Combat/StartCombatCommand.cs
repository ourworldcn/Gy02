using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Combat
{
    public class StartCombatCommand: PropertyChangeCommandBase
    {
        public StartCombatCommand()
        {

        }
    }

    public class StartCombatHandler : SyncCommandHandlerBase<StartCombatCommand>
    {
        public StartCombatHandler()
        {

        }

        public override void Handle(StartCombatCommand command)
        {
        }
    }
}
