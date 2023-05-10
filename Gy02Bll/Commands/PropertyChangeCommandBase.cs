using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class PropertyChangeCommandBase : SyncCommandBase
    {
        List<GamePropertyChangeItem<object>> _Changes;

        public List<GamePropertyChangeItem<object>> Changes
        {
            get => LazyInitializer.EnsureInitialized(ref _Changes);

            set => _Changes = value;
        }
    }
}
