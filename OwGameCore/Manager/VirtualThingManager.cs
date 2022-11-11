using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Manager
{
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class VirtualThingManager
    {
        public VirtualThingManager()
        {
        }

        void Initialize()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        }
    }
}
