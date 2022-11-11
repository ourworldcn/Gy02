using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Manager
{
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameCharManager
    {
        public GameCharManager()
        {
        }

    }
}
