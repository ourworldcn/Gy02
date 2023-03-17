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
    /// <summary>
    /// TODO 标记强类型对象的模板Id。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TemplateIdAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tidString"><see cref="Guid"/>的字符串表述形式。</param>
        public TemplateIdAttribute(string tidString)
        {
            _TemplateId = Guid.Parse(tidString);

            // Implement code here

        }

        /// <summary>
        /// See the attribute guidelines at 
        /// http://go.microsoft.com/fwlink/?LinkId=85236
        /// </summary>
        readonly Guid _TemplateId;
        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId
        {
            get { return _TemplateId; }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class VirtualThingManager
    {
        public VirtualThingManager(IServiceProvider service)
        {
            _Service = service;
            Initialize();
        }

        void Initialize()
        {
            Cache = _Service.GetRequiredService<GameObjectCache>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        }

        public GameObjectCache Cache { get; protected set; }

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
