using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.TemplateDb.Entity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Managers
{
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class TemplateManager
    {
        public TemplateManager(GY02TemplateContext dbContext)
        {
            DbContext = dbContext;
            Initialize();
        }

        private void Initialize()
        {
            lock (_Locker)
            {
                var ary = DbContext.ThingTemplates.ToArray();
                _Id2Template = new ConcurrentDictionary<Guid, GameThingTemplate>(ary.ToDictionary(c => c.Id));
            }
        }

        private readonly object _Locker = new();

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        ConcurrentDictionary<Guid, GameThingTemplate> _Id2Template;
        /// <summary>
        /// 获取所有模板的字典。键时模板id,值模板对象。
        /// </summary>
        public IReadOnlyDictionary<Guid, GameThingTemplate> Id2Template
        {
            get
            {
                lock (_Locker)
                    return _Id2Template;
            }
        }

        public GameThingTemplate GetTemplateFromId(Guid id)
        {
            lock (_Locker)
                return _Id2Template.GetValueOrDefault(id);
        }
    }
}
