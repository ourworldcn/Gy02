using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY02.Store;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Manager;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// 
    /// </summary>
    public class RawTemplateOptions : Collection<RawTemplate>
    {
        public RawTemplateOptions() : base()
        {

        }

    }

    public class TemplateManagerOptions : IOptions<TemplateManagerOptions>
    {
        public TemplateManagerOptions() { }

        public TemplateManagerOptions Value => this;
    }
    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class TemplateManager : GameManagerBase<TemplateManagerOptions, TemplateManager>
    {
        public TemplateManager(IOptions<TemplateManagerOptions> options, GY02TemplateContext dbContext, ILogger<TemplateManager> logger, IHostApplicationLifetime lifetime, IOptionsMonitor<RawTemplateOptions> rawTemplateOptions)
            : base(options, logger)
        {
            DbContext = dbContext;
            logger.LogDebug("上线:模板管理器。");
            _Lifetime = lifetime;
            _RawTemplateOptions = rawTemplateOptions;
            _RawTemplateOptionsChangedMonitor = _RawTemplateOptions.OnChange(RawTemplateOptionsChanged);
            Initialize();
        }

        private void RawTemplateOptionsChanged(RawTemplateOptions arg1, string arg2)
        {

        }

        IDisposable _RawTemplateOptionsChangedMonitor;
        IOptionsMonitor<RawTemplateOptions> _RawTemplateOptions;
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
                var path = Path.Combine(AppContext.BaseDirectory, "数据表", file);
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

                try
                {
                    //using var br = new StreamReader(streamTemplate);
                    //var str = br.ReadToEnd();
                    var dic = _RawTemplateOptions.CurrentValue.Select(c => (GameTemplate<TemplatePropertiesString>)c).ToDictionary(c => c.Id);
                    _Id2Template2 = new ConcurrentDictionary<Guid, GameTemplate<TemplatePropertiesString>>(dic);
                }
                catch (Exception excp)
                {
                    Logger.LogWarning(excp, "读取模板数据出现错误。");
                }

            });

        }

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        ConcurrentDictionary<Guid, GameThingTemplate> _Id2Template;
        ConcurrentDictionary<Guid, GameTemplate<TemplatePropertiesString>> _Id2Template2;
        public ConcurrentDictionary<Guid, GameTemplate<TemplatePropertiesString>> Id2Template2
        {
            get
            {
                _InitTask.Wait();
                return _Id2Template2;
            }
        }

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

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            _RawTemplateOptionsChangedMonitor?.Dispose();
            _RawTemplateOptionsChangedMonitor = null;
            base.Dispose(disposing);
        }
        #endregion IDisposable
    }

    public static class TemplateManagerExtensions
    {
        public static IServiceCollection AddTemplateManager(this IServiceCollection services)
        {
            return services;
        }
    }
}
