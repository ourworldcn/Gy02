using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY02.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Game.Manager;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Managers
{
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class TemplateManager
    {
        public TemplateManager(GY02TemplateContext dbContext, ILogger<TemplateManager> logger, IHostApplicationLifetime lifetime)
        {
            DbContext = dbContext;
            Logger = logger;
            logger.LogDebug("上线:模板管理器。");
            _Lifetime = lifetime;
            Initialize();

        }

        IHostApplicationLifetime _Lifetime;
        Task _InitTask;
        private void Initialize()
        {
            //    var ary = DbContext.ThingTemplates.ToArray();
            //    _Id2Template = new ConcurrentDictionary<Guid, GY02ThingTemplate>(ary.ToDictionary(c => c.Id));
            Interlocked.CompareExchange(ref _Id2Template, new ConcurrentDictionary<Guid, GameThingTemplate>(), null);

            _Lifetime.ApplicationStarted.Register(() =>
            {
            });
            _InitTask = Task.Run(() =>
            {
                var file = $"TemplateData.json";
                var path = Path.Combine(AppContext.BaseDirectory, "数据表\\", file);
                using var stream = File.OpenRead(path);
                var opt = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, };

                var jn = JsonSerializer.Deserialize<JsonElement>(stream, opt);
                foreach (var item in jn.EnumerateArray())
                {
                    var tt = new GameThingTemplate()
                    {
                        Id = item.GetProperty("Id").GetGuid(),
                        Remark = item.TryGetProperty("Remark", out var tmp) ? tmp.GetString() : null,
                    };
                    tt.JsonObjectString = item.GetRawText();
                    _Id2Template[tt.Id] = tt;
                }
                //分类
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        /// <summary>
        /// 日志接口。
        /// </summary>
        public ILogger<TemplateManager> Logger { get; }

        ConcurrentDictionary<Guid, GameThingTemplate> _Id2Template;
        /// <summary>
        /// 获取所有模板的字典。键时模板id,值模板对象。
        /// </summary>
        public IReadOnlyDictionary<Guid, GameThingTemplate> Id2Template
        {
            get
            {
                _InitTask.Wait();
                return _Id2Template;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GameThingTemplate GetTemplateFromId(Guid id)
        {
            _InitTask.Wait();
            return _Id2Template.GetValueOrDefault(id);
        }
    }

    public static class TemplateManagerExtensions
    {
        public static IServiceCollection AddTemplateManager(this IServiceCollection services)
        {
            return services;
        }
    }
}
