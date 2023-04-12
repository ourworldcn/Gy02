using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Combat
{
    public class CombatMarkCommand: SyncCommandBase
    {
        public CombatMarkCommand() { }
    }

    public class CombatMarkHandler : SyncCommandHandlerBase<CombatMarkCommand>
    {
        public CombatMarkHandler()
        {

        }

        public override void Handle(CombatMarkCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
