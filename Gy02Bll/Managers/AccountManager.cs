﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    public class AccountManagerOptions : IOptions<AccountManagerOptions>
    {
        public AccountManagerOptions()
        {
        }

        public AccountManagerOptions Value => this;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    }

    /// <summary>
    /// 账号管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameAccountManager : GameManagerBase<AccountManagerOptions>
    {
        #region 构造函数及相关

        public GameAccountManager(IOptions<AccountManagerOptions> options, ILogger<DataObjectManager> logger, OwServerMemoryCache cache, IServiceProvider service)
        {
            _Service = service;
            Options = options.Value;
            _Logger = logger;
            _Cache = cache;
            _Scheduler = (OwScheduler)service.GetRequiredService<IEnumerable<IHostedService>>().First(c => c is OwScheduler);
            Initialize();
        }

        /// <summary>
        /// 内部初始化函数。
        /// </summary>
        [SuppressMessage("Performance", "CA1822:将成员标记为 static", Justification = "<挂起>")]
        private void Initialize()
        {
        }

        #endregion 构造函数及相关

        IServiceProvider _Service;

        public IServiceProvider Service => _Service;

        ThingManager _ThingManager;
        /// <summary>
        /// 获取基础管理器。
        /// 其中账号的Id的<see cref="Guid.ToString"/>后的字符串是键。
        /// </summary>
        public ThingManager ThingManager => _ThingManager ??= _Service.GetRequiredService<ThingManager>();

        /// <summary>
        /// 票据到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<Guid, string> _Token2Key = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// 角色到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<Guid, string> _CharId2Key = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// 登录名到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<string, string> _LoginNameId2Key = new ConcurrentDictionary<string, string>();

        private ILogger<DataObjectManager> _Logger;

        OwScheduler _Scheduler;
        /// <summary>
        /// 任务计划对象。
        /// </summary>
        public OwScheduler Scheduler { get => _Scheduler; }

        OwServerMemoryCache _Cache;
        /// <summary>
        /// 缓存对象。
        /// </summary>
        public OwServerMemoryCache Cache { get => _Cache; }

        /// <summary>
        /// 获取指定角色Id的角色是否在线。
        /// </summary>
        /// <param name="charId">角色Id。</param>
        /// <returns></returns>
        public bool IsOnline(Guid charId) => _CharId2Key.ContainsKey(charId);

        public bool LoadFromCharId(Guid userId)
        {
            if (_CharId2Key.TryGetValue(userId, out var key))
            {
                using var dwKey = DisposeHelper.Create(ThingManager.Cache.Exit, key);
                if (dwKey.IsEmpty)
                    return false;
            }
            GY02UserContext db = null;
            var guEntity = ThingManager.GetOrLoadThing<GY02UserContext, OrphanedThing>(userId.ToString(), c => c.Id == userId, c => { }, ref db);
            var gcEntity = db.Set<VirtualThing>().FirstOrDefault(c => c.ExtraString == userId.ToString());
            guEntity.RuntimeProperties["CurrentChar"] = gcEntity;
            return default;
        }

        /// <summary>
        /// 计算密码的Hash值。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static private byte[] GetHash(string str)
        {
            var bin = Encoding.UTF8.GetBytes(str);
            return SHA256.HashData(bin);
        }

        /// <summary>
        /// 获取指定key的用户对象。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="thing"></param>
        /// <returns>获取指定键的对象，若没有找到则返回<see cref="DisposeHelper{T}.IsEmpty"/>为true。</returns>
        public DisposeHelper<object> Get(object key, out OrphanedThing thing)
        {
            DisposeHelper<object> result;
            result = DisposeHelper.Create(ThingManager.Cache.TryEnter, ThingManager.Cache.Exit, key, Options.Timeout);
            if (!result.IsEmpty)    //若锁定成功
            {
                thing = ThingManager.Get(key) as OrphanedThing;
                if (thing is null)  //若没有找到
                    using (result)
                        return DisposeHelper.Empty<object>();
                else
                    return result;
            }
            else //若锁定失败
                thing = default;
            return result;
        }

        /// <summary>
        /// 登录。
        /// </summary>
        /// <param name="uid">登录名。</param>
        /// <param name="pwd">密码。</param>
        /// <returns></returns>
        public DisposeHelper<object> Login(string uid, string pwd, out OrphanedThing thing)
        {
            DisposeHelper<object> dwKey;
            object result = null;
            if (_LoginNameId2Key.TryGetValue(uid, out var key)) //若找到了已经加载的用户
            {
                dwKey = Get(key, out thing);
                if (!dwKey.IsEmpty)  //若锁定成功
                {
                    result = ThingManager.Get(key);
                    if (result is not null) //若找到
                        ;
                }
            }
            else //若未加载
            {

            }
            var hash = GetHash(pwd);

            thing = default;
            return default;
        }

        /// <summary>
        /// 创建一个账号。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="user"></param>
        /// <returns>账号的id的锁定结构。</returns>
        public DisposeHelper<object> Create(string loginName, string pwd, Action<DbContext, OrphanedThing> initCallback, out OrphanedThing user)
        {
            using var dwUid = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, loginName, Options.Timeout);
            if (dwUid.IsEmpty)   //若锁定失败
                throw new TimeoutException();
            if (_LoginNameId2Key.ContainsKey(loginName))   //若已经存在
                throw new InvalidOperationException();
            var db = _Service.GetRequiredService<IDbContextFactory<GY02UserContext>>().CreateDbContext();
            if (db.Set<OrphanedThing>().Any(c => c.ExtraString == loginName))  //若数据库中已经存在
            {
                using var tmp = db;
                throw new InvalidOperationException();
            }
            user = new OrphanedThing();
            initCallback(db, user);
            var mceo = new MemoryCacheEntryOptions();
            mceo.SetSlidingExpiration(TimeSpan.FromMinutes(1)).RegisterPostEvictionCallback((cKey, cValue, cReason, cState) => { });

            _Cache.Set(user.IdString, user, mceo);
            user.RuntimeProperties["DbContext"] = db;

            return new DisposeHelper<object>();
        }

        #region IDisposable接口相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _ThingManager = null;
                _Service = null;
                _Token2Key = null;
                _LoginNameId2Key = null;
                _CharId2Key = null;
            }
            base.Dispose(disposing);
        }

        #endregion IDisposable接口相关

    }
}
