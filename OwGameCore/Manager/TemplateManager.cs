using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY02.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.Store;
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
        public TemplateManager(GY02TemplateContext dbContext, ILogger<TemplateManager> logger)
        {
            DbContext = dbContext;
            Logger = logger;
            Initialize();
            logger.LogDebug("上线:模板管理器。");
        }

        private void Initialize()
        {
            lock (_Locker)
            {
                var ary = DbContext.ThingTemplates.ToArray();
                _Id2Template = new ConcurrentDictionary<Guid, GY02ThingTemplate>(ary.ToDictionary(c => c.Id));
            }
        }

        private readonly object _Locker = new object();

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        /// <summary>
        /// 日志接口。
        /// </summary>
        public ILogger<TemplateManager> Logger { get; }

        ConcurrentDictionary<Guid, GY02ThingTemplate> _Id2Template;
        /// <summary>
        /// 获取所有模板的字典。键时模板id,值模板对象。
        /// </summary>
        public IReadOnlyDictionary<Guid, GY02ThingTemplate> Id2Template
        {
            get
            {
                lock (_Locker)
                    return _Id2Template;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GY02ThingTemplate GetTemplateFromId(Guid id)
        {
            lock (_Locker)
                return _Id2Template.GetValueOrDefault(id);
        }
    }
}
