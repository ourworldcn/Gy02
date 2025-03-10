﻿using GuangYuan.GY001.TemplateDb;
using GY02.Publisher;
using GY02.TemplateDb;
using GY02.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

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

    public class GameTemplateManagerOptions : IOptions<GameTemplateManagerOptions>
    {
        public GameTemplateManagerOptions() { }

        public GameTemplateManagerOptions Value => this;
    }

    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class GameTemplateManager : GameManagerBase<GameTemplateManagerOptions, GameTemplateManager>
    {
        #region 静态成员

        /// <summary>
        /// 验证模板数据。
        /// </summary>
        /// <param name="rawTemplate"></param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        public static Dictionary<Guid, TemplateStringFullView> GetTemplateFullviews(IEnumerable<RawTemplate> rawTemplate)
        {
            List<Exception> exceptions = new List<Exception>();
            var result = new Dictionary<Guid, TemplateStringFullView>();
            HashSet<int> gids = new HashSet<int>();
            foreach (RawTemplate raw in rawTemplate)
            {
                try
                {
                    var tmp = raw.GetJsonObject<TemplateStringFullView>();
                    result.Add(raw.Id, tmp);
                    //验证GId重复问题
                    if (tmp.Gid.HasValue && !gids.Add(tmp.Gid.Value)) throw new InvalidDataException($"重复的Gid={tmp.Gid.Value}");
                }
                catch (Exception excp)
                {
                    var excpOutter = new InvalidDataException($"解析模板数据时出错：{Environment.NewLine}Id = {raw.Id}{Environment.NewLine}String = {raw.PropertiesString}{Environment.NewLine}{excp.Message}", excp);
                    exceptions.Add(excpOutter);
                    break;
                }
            }
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
            return result;
        }

        static ConcurrentDictionary<Guid, Type> _TypeGuid2Type;
        /// <summary>
        /// 键是模板中TypeGuid属性，值是改模板创建的对象的实体类型。
        /// </summary>
        public static ConcurrentDictionary<Guid, Type> TypeGuid2Type
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _TypeGuid2Type, () =>
                {
                    var coll = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes()).Where(c => !c.IsAbstract && c.IsAssignableTo(typeof(GameEntityBase)));
                    var tmp = new ConcurrentDictionary<Guid, Type>(coll.ToDictionary(c => c.GUID));
                    return tmp;
                });
            }
        }

        /// <summary>
        /// 获取模板的生成类的类型。
        /// </summary>
        /// <param name="fullView"></param>
        /// <returns>如果没有找到可能返回null。</returns>
        public static Type GetTypeFromTemplate(TemplateStringFullView fullView)
        {
            var result = TypeGuid2Type.GetValueOrDefault(fullView.TypeGuid);    //获取实例类型
            if (result is not null && fullView.SubTypeGuid is not null) //若是泛型类
            {
                var sub = TypeGuid2Type.GetValueOrDefault(fullView.SubTypeGuid.Value);
                result = result.MakeGenericType(sub);
            }
            return result;
        }

        #endregion 静态成员

        /// <summary>
        /// 获取模板的生成类的类型。
        /// </summary>
        /// <param name="tid"></param>
        /// <returns></returns>
        public Type GetTypeFromTId(Guid tid)
        {
            var tt = GetRawTemplateFromId(tid);
            var fv = tt?.GetJsonObject<TemplateStringFullView>();
            if (fv is null)
                return null;
            return GetTypeFromTemplate(fv);
        }

        public GameTemplateManager(IOptions<GameTemplateManagerOptions> options, GY02TemplateContext dbContext, ILogger<GameTemplateManager> logger, IHostApplicationLifetime lifetime,
            IOptionsMonitor<RawTemplateOptions> rawTemplateOptions, IConfiguration configuration)
            : base(options, logger)
        {
            DbContext = dbContext;
            _Lifetime = lifetime;
            _RawTemplateOptions = rawTemplateOptions;
            _RawTemplateOptionsChangedMonitor = _RawTemplateOptions.OnChange(RawTemplateOptionsChanged);
            Initialize();
            logger.LogDebug("上线:模板管理器。");
            _Configuration = configuration;
            var ss = _Configuration.GetSection("SensitiveWords").GetChildren();
            _SensitiveWords = new HashSet<string>(ss.Select(c => c.Value));
            if (_SensitiveWords.Count > 0)
            {
                var tmp = _SensitiveWords.Where(c => !string.IsNullOrEmpty(c));
                if (tmp.Any())
                    _MinLength = tmp.Min(s => s.Length);
                _MaxLength = _SensitiveWords.Max(s => s.Length);
            }
        }

        private void RawTemplateOptionsChanged(RawTemplateOptions arg1, string arg2)
        {
            _Id2RawTemplate = null;
            _Id2FullView = null;
        }

        IDisposable _RawTemplateOptionsChangedMonitor;
        IOptionsMonitor<RawTemplateOptions> _RawTemplateOptions;
        IHostApplicationLifetime _Lifetime;
        IConfiguration _Configuration;

        HashSet<string> _SensitiveWords;
        int _MinLength = 0;
        int _MaxLength = 0;

        private void Initialize()
        {
            _Lifetime.ApplicationStarted.Register(() =>
            {
            });
            try
            {
                Task.Run(() => GetTemplatesFromGenus(string.Empty));
            }
            catch (Exception excp)
            {
                Logger.LogWarning(excp, "读取模板数据出现错误。");
                throw;
            }
        }

        #region 敏感词

        /// <summary>
        /// 是否又碰到后脚跟的G点了。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool IsOrgasm(string str, out string result)
        {
            result = null;
            if (_MinLength == 0 || _MaxLength == 0)
            {
                return false;
            }
            for (int i = 0; i <= str.Length - _MinLength; i++)
            {
                for (int j = _MinLength; j <= _MaxLength; j++)
                {
                    if (i + j > str.Length) break;
                    var tmp = str[i..(i + j)];
                    if (_SensitiveWords.Contains(tmp))
                    {
                        result = tmp;
                        return true;
                    }
                }
            }
            return true;
        }
        #endregion 敏感词

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        ConcurrentDictionary<Guid, RawTemplate> _Id2RawTemplate;

        /// <summary>
        /// 模板的原始形态。
        /// </summary>
        public ConcurrentDictionary<Guid, RawTemplate> Id2RawTemplate
        {
            get
            {
                Dictionary<Guid, RawTemplate> dic;
                try
                {
                    var tmp = _RawTemplateOptions.CurrentValue;
                    dic = tmp.ToDictionary(c => c.Id);
                }
                catch (Exception)
                {
                    throw;
                }
                LazyInitializer.EnsureInitialized(ref _Id2RawTemplate, () => new ConcurrentDictionary<Guid, RawTemplate>(dic));
                return _Id2RawTemplate;
            }
        }

        /// <summary>
        /// 记录所有模板。
        /// </summary>
        ConcurrentDictionary<Guid, TemplateStringFullView> _Id2FullView;
        /// <summary>
        /// 所有模板全量视图。
        /// </summary>
        public ConcurrentDictionary<Guid, TemplateStringFullView> Id2FullView
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _Id2FullView,
                    () => new ConcurrentDictionary<Guid, TemplateStringFullView>(Id2RawTemplate.Select(c => c.Value.GetJsonObject<TemplateStringFullView>()).ToDictionary(c => c.TemplateId)));
                return _Id2FullView;
            }
        }


        /// <summary>
        /// 获取指定Id的原始模板。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RawTemplate GetRawTemplateFromId(Guid id)
        {
            return Id2RawTemplate.GetValueOrDefault(id);
        }

        /// <summary>
        /// 返回完整视图。
        /// </summary>
        /// <param name="tid"></param>
        /// <returns>完整视图对象，如果没有找到则返回null。此时<see cref="OwHelper.GetLastError"/>将返回具体错误。</returns>
        public TemplateStringFullView GetFullViewFromId(Guid tid)
        {
            var result = Id2FullView.GetValueOrDefault(tid);
            if (result == null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定Id的模板，TId={tid}");
            }
            return result;
        }

        /// <summary>
        /// 获取虚拟物的实体。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="type">返回值的实际类型。</param>
        /// <returns>返回的是<see cref="GameEntityBase"/>的派生对象。如果没找到则可能返回null。</returns>
        public GameEntityBase GetEntityBase(VirtualThing thing, out Type type)
        {
            var tt = Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            if (tt is null) goto fail;
            type = GetTypeFromTemplate(tt);
            return thing.GetJsonObject(type) as GameEntityBase;
        fail:
            type = null;
            return null;
        }

        /// <summary>
        /// 获取指定虚拟物的模板和实体。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="entity">实际的实体类型。</param>
        /// <param name="fullView"></param>
        /// <returns></returns>
        public bool GetEntityAndTemplate(VirtualThing thing, out GameEntityBase entity, out TemplateStringFullView fullView)
        {
            fullView = Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            if (fullView is null) goto error;
            var type = GetTypeFromTemplate(fullView);
            if (type is null) goto error;
            entity = thing.GetJsonObject(type) as GameEntityBase;
            return entity is not null;
        error:
            entity = null;
            return false;
        }

        ILookup<string, TemplateStringFullView> _Genus2Templates;

        /// <summary>
        /// 获取指定类属的所有模板的字典(Id,模板)。
        /// </summary>
        /// <param name="genus"></param>
        /// <returns>指定类属的所有模板集合，如果找不到该序列，则返回空序列。</returns>
        public IEnumerable<TemplateStringFullView> GetTemplatesFromGenus(string genus)
        {
            LazyInitializer.EnsureInitialized(ref _Genus2Templates, () =>
            {
                var coll = from fv in Id2FullView.Values
                           where fv.Genus is not null
                           from genus in fv.Genus
                           select (genus, fv);
                var result = coll.ToLookup(c => c.genus, c => c.fv);
                return result;
            });
            var result = _Genus2Templates[genus];
            if (!result.Any()) OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定类属的模板，Genus = {genus} 。");
            return result;
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
