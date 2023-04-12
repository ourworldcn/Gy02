using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Combat
{
    public class EndCombatCommand : PropertyChangeCommandBase
    {
        public EndCombatCommand()
        {

        }
    }

    public class EndCombatHandler : SyncCommandHandlerBase<EndCombatCommand>
    {
        public EndCombatHandler()
        {

        }

        public override void Handle(EndCombatCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
