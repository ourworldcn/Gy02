using Microsoft.Extensions.DependencyInjection;
using OW.Game.Caching;
using OW.Game.Managers;
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
        public VirtualThingManager(GameObjectCache cache, IServiceProvider service)
        {
            Cache = cache;
            _Service = service;
        }

        void Initialize()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        }

        public GameObjectCache Cache { get; }

        IServiceProvider _Service;

        public DisposeHelper<string> Load(Guid charId, out VirtualThing thing)
        {
            var result = DisposeHelper.Empty<string>();
            thing = default;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public VirtualThing CreateThing(GameThingTemplate template)
        {
            var result = new VirtualThing { };
            var gtm = _Service.GetRequiredService<TemplateManager>();
            //template.GetJsonObject<Gy02TemplateJO>();
            //GameThingTemplate template1;
            return result;
        }
    }
}
