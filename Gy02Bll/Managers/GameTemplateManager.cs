﻿using GuangYuan.GY001.TemplateDb;
using GY02.Publisher;
using GY02.TemplateDb;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Store;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conditionalItem"></param>
        /// <param name="value"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public bool TryGetValueFromConditionalItem(GeneralConditionalItem conditionalItem, out object value, params object[] objects)
        {
            var result = false;
            switch (conditionalItem.Operator)
            {
                case "ToInt32":
                    {
                        var pName = conditionalItem.Args[0];
                        var obj = objects[0];
                        var pi = obj.GetType().GetProperty(pName);
                        if (pi is null)
                        {
                            OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定属性，属性名={pName}");
                            break;
                        }
                        var tmp = pi.GetValue(obj);
                        value = Convert.ToInt32(tmp);
                        result = true;
                    }
                    break;
                case "GetBuyedCount":
                    {
                        var now = OwHelper.WorldNow;
                        var gameChar = objects[0] as GameChar;
                        if (gameChar is null)
                        {
                            value = 0; break;
                        }
                        if (!Guid.TryParse(conditionalItem.Args[0].ToString(), out var tid)) //商品的TId
                        {
                            value = 0; break;

                        }
                        var tt = GetFullViewFromId(tid);
                        if (!tt.ShoppingItem.Period.IsValid(now, out var start))
                        {
                            value = 0; break;
                        }

                        var list = gameChar.ShoppingHistory;
                        var val = list.Where(c => c.TId == tid && c.DateTime >= start && c.DateTime <= now).Sum(c => c.Count);  //如果source不包含任何元素，则Sum(IEnumerable<Decimal>)方法返回零。
                        value = Convert.ToInt32(val);
                        result = true;
                    }
                    break;
                case "ModE":
                    {
                        //获取属性值
                        var pName = conditionalItem.PropertyName;
                        var obj = objects[0];
                        var pi = obj.GetType().GetProperty(pName);
                        if (pi is null)
                        {
                            OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定属性，属性名={pName}");
                            break;
                        }
                        var tmp = pi.GetValue(obj);
                        var val = Convert.ToDecimal(tmp);
                        if (!OwConvert.TryToDecimal(conditionalItem.Args[0], out var arg0) || !OwConvert.TryToDecimal(conditionalItem.Args[1], out var arg1))
                        {
                            OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                            OwHelper.SetLastErrorMessage($"ModE要有两个参数都是数值型。");
                            result = false;
                            break;
                        }
                        else
                            OwHelper.SetLastError(ErrorCodes.NO_ERROR);
                        value = val % arg0 == arg1;
                        result = true;
                    }
                    break;
                default:
                    break;
            }
            value = default;
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
