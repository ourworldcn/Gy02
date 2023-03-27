using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class PropertyChangeCommandBase : SyncCommandBase
    {
        List<GamePropertyChangeItem<object>> _Changes;

        public List<GamePropertyChangeItem<object>> Changes
        {
            get
            {
                if (_Changes is null)
                    Interlocked.CompareExchange(ref _Changes, new List<GamePropertyChangeItem<object>> { }, null);
                return _Changes;
            }

            set => _Changes = value;
        }
    }
}
