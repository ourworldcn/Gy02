using Microsoft.Extensions.DependencyInjection;
using OW.Game.Caching;
using OW.Game.Store;
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
        public VirtualThingManager(GameObjectCache cache)
        {
            Cache = cache;
        }

        void Initialize()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        }

        public GameObjectCache Cache { get; }

        public DisposeHelper<string> Load(Guid charId, out VirtualThing thing)
        {
            var result = DisposeHelper.Empty<string>();
            thing = default;
            return result;
        }

    }
}
