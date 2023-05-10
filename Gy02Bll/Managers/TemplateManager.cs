using GuangYuan.GY001.TemplateDb;
using GY02.Publisher;
using GY02.TemplateDb;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Store;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

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
        #region 静态成员

        public static Dictionary<Guid, TemplateStringFullView> GetTemplateFullviews(IEnumerable<RawTemplate> rawTemplate)
        {
            List<Exception> exceptions = new List<Exception>();
            var result = new Dictionary<Guid, TemplateStringFullView>();
            foreach (RawTemplate raw in rawTemplate)
            {
                try
                {
                    var tmp = raw.GetJsonObject<TemplateStringFullView>();
                    result.Add(raw.Id, tmp);
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
                    var coll = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes()).Where(c => !c.IsAbstract && c.IsAssignableTo(typeof(OwGameEntityBase)));
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

        public TemplateManager(IOptions<TemplateManagerOptions> options, GY02TemplateContext dbContext, ILogger<TemplateManager> logger, IHostApplicationLifetime lifetime,
            IOptionsMonitor<RawTemplateOptions> rawTemplateOptions)
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
            var raws = new ConcurrentDictionary<Guid, RawTemplate>(arg1.ToDictionary(c => c.Id));
            var fullViews = new ConcurrentDictionary<Guid, TemplateStringFullView>(raws.Select(c => c.Value.GetJsonObject<TemplateStringFullView>()).ToDictionary(c => c.TemplateId));

            lock (this)
            {
                _Id2RawTemplate = raws;
                _Id2FullView = fullViews;
            }
        }

        IDisposable _RawTemplateOptionsChangedMonitor;
        IOptionsMonitor<RawTemplateOptions> _RawTemplateOptions;
        IHostApplicationLifetime _Lifetime;
        Task _InitTask;

        private void Initialize()
        {
            //    var ary = DbContext.ThingTemplates.ToArray();
            //    _Id2Template = new ConcurrentDictionary<Guid, GY02ThingTemplate>(ary.ToDictionary(c => c.Id));

            _Lifetime.ApplicationStarted.Register(() =>
            {
            });
            _InitTask = Task.Run(() =>
            {
                try
                {
                    lock (this)
                    {
                        _Id2RawTemplate = new ConcurrentDictionary<Guid, RawTemplate>(_RawTemplateOptions.CurrentValue.ToDictionary(c => c.Id));
                        _Id2FullView = new ConcurrentDictionary<Guid, TemplateStringFullView>(_Id2RawTemplate.Select(c => c.Value.GetJsonObject<TemplateStringFullView>()).ToDictionary(c => c.TemplateId));
                    }
                }
                catch (Exception excp)
                {
                    Logger.LogWarning(excp, "读取模板数据出现错误。");
                    throw;
                }

            });

        }

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        ConcurrentDictionary<Guid, RawTemplate> _Id2RawTemplate;
        public ConcurrentDictionary<Guid, RawTemplate> Id2RawTemplate
        {
            get
            {
                _InitTask.Wait();
                return _Id2RawTemplate;
            }
        }

        ConcurrentDictionary<Guid, TemplateStringFullView> _Id2FullView;
        /// <summary>
        /// 所有模板全量视图。
        /// </summary>
        public ConcurrentDictionary<Guid, TemplateStringFullView> Id2FullView
        {
            get
            {
                _InitTask.Wait();
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
        /// <returns>返回的是<see cref="OwGameEntityBase"/>的派生对象。如果没找到则可能返回null。</returns>
        public OwGameEntityBase GetEntityBase(VirtualThing thing, out Type type)
        {
            var tt = Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            if (tt is null) goto fail;
            type = GetTypeFromTemplate(tt);
            return thing.GetJsonObject(type) as OwGameEntityBase;
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
        public bool GetEntityAndTemplate(VirtualThing thing, out OwGameEntityBase entity, out TemplateStringFullView fullView)
        {
            fullView = Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            if (fullView is null) goto error;
            var type = GetTypeFromTemplate(fullView);
            if (type is null) goto error;
            entity = thing.GetJsonObject(type) as OwGameEntityBase;
            return entity is not null;
        error:
            entity = null;
            return false;
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            _RawTemplateOptionsChangedMonitor?.Dispose();
            _RawTemplateOptionsChangedMonitor = null;
            base.Dispose(disposing);
        }
        #endregion IDisposable

        #region 孵化相关
        /// <summary>
        /// 返回孵化的模板信息。
        /// </summary>
        /// <param name="parentGenus"></param>
        /// <returns>(孵化模板，动物模板，皮肤模板)</returns>
        public (TemplateStringFullView, GameDice, GameDice) GetFuhuaInfo(IEnumerable<string> parentGenus)
        {
            var fuhua = Id2FullView.Values.Where(c => c.Fuhua is not null).First(c => GetGenus(c.Fuhua).SequenceEqual(parentGenus));
            if (fuhua is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定类属组合的孵化信息{parentGenus}");
                return (null, null, null);
            }
            var mounts = GetFullViewFromId(fuhua.Fuhua.DiceTId1)?.Dice;
            if (mounts is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定的卡池模板，TId={fuhua.Fuhua.DiceTId1}");
                return (null, null, null);
            }
            var pifus = GetFullViewFromId(fuhua.Fuhua.DiceTId2)?.Dice;
            if (pifus is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定的卡池模板，TId={fuhua.Fuhua.DiceTId1}");
                return (null, null, null);
            }
            return (fuhua, mounts, pifus);
        }

        /// <summary>
        /// 获取孵化信息中双亲的类属信息。
        /// </summary>
        /// <param name="fuhua"></param>
        /// <returns>返回值已经排序。</returns>
        public static IEnumerable<string> GetGenus(FuhuaInfo fuhua)
        {
            var result = new List<string>()
            {
               fuhua.Parent1Conditional.First().Genus[0],   //孵化信息必须是这个结构
               fuhua.Parent2Conditional.First().Genus[0],
            };
            result.Sort();
            return result;
        }

        #endregion 孵化相关
    }

    public static class TemplateManagerExtensions
    {
        public static IServiceCollection AddTemplateManager(this IServiceCollection services)
        {
            return services;
        }
    }
}
