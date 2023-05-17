using GY02.Commands;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.Server;
using OW.SyncCommand;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameAccountStoreOptions : IOptions<GameAccountStoreOptions>
    {
        public GameAccountStoreOptions()
        {

        }

        public GameAccountStoreOptions Value => this;

        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="UnlockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> LockCallback { get; set; } = SingletonLocker.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="LockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.Exit(object)"/>。
        /// </summary>
        public Action<object> UnlockCallback { get; set; } = SingletonLocker.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="SingletonLocker.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = SingletonLocker.IsEntered;

        /// <summary>
        /// 默认的锁定超时时间。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

    }

    /// <summary>
    /// 存储及索引服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameAccountStore : GameManagerBase<GameAccountStoreOptions, GameAccountStore>
    {
        public GameAccountStore(IOptions<GameAccountStoreOptions> options, ILogger<GameAccountStore> logger, IDbContextFactory<GY02UserContext> contextFactory,
            IHostApplicationLifetime lifetime, IServiceProvider service) : base(options, logger)
        {
            _ContextFactory = contextFactory;
            _Lifetime = lifetime;
            Task.Factory.StartNew(SaveCallback, TaskCreationOptions.LongRunning);
            _Service = service;
        }

        IDbContextFactory<GY02UserContext> _ContextFactory;
        IHostApplicationLifetime _Lifetime;
        IServiceProvider _Service;

        /// <summary>
        /// 记录所有用户对象。
        /// </summary>
        public ConcurrentDictionary<string, GameUser> _Key2User = new ConcurrentDictionary<string, GameUser>();

        /// <summary>
        /// 票据到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<Guid, string> _Token2Key = new ConcurrentDictionary<Guid, string>();
        public ConcurrentDictionary<Guid, string> Token2Key { get => _Token2Key; }

        /// <summary>
        /// 角色到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<Guid, string> _CharId2Key = new ConcurrentDictionary<Guid, string>();
        public ConcurrentDictionary<Guid, string> CharId2Key { get => _CharId2Key; }

        /// <summary>
        /// 登录名到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<string, string> _LoginName2Key = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> LoginName2Key { get => _LoginName2Key; }

        /// <summary>
        /// 需要保存的账号的key。
        /// </summary>
        ConcurrentDictionary<string, string> _Queue = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 后台保存函数。
        /// </summary>
        void SaveCallback()
        {
            while (!_Lifetime.ApplicationStopped.IsCancellationRequested)
            {
                foreach (var item in _Queue)    //遍历优先保存项
                {
                    using var dw = DisposeHelper.Create(Lock, Unlock, item.Key, TimeSpan.Zero);
                    if (dw.IsEmpty)
                        continue;
                    if (!_Queue.Remove(item.Key, out _))  //若已经并发去除
                        continue;
                    var gu = _Key2User.GetValueOrDefault(item.Key);
                    if (gu is null) continue;
                    try
                    {
                        gu.GetDbContext().SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException excp)
                    {
                        _Queue.TryAdd(item.Key, null);  //加入队列以备未来写入
                        var ids = string.Join(',', excp.Entries.OfType<VirtualThing>().Select(c => c.IdString));
                        var tids = string.Join(',', excp.Entries.OfType<VirtualThing>().Select(c => c.ExtraGuid));
                        Logger.LogWarning(excp, $"保存数据时出现并发错误——ids:{ids}。tids:{tids}");
                    }
                    catch (Exception excp)
                    {
                        _Queue.TryAdd(item.Key, null);  //加入队列以备未来写入
                        Logger.LogWarning(excp, $"保存数据时出错——{excp.Message}。{excp.InnerException?.Message}。");
                    }
                }
                //计算超时
                if (_Key2User.Any())    //若有账号
                {
                    using var scope = _Service.CreateScope();
                    var svcCommand = scope.ServiceProvider.GetRequiredService<SyncCommandManager>();
                    foreach (var item in _Key2User) //遍历所有账号
                    {
                        using var dw = DisposeHelper.Create(Lock, Unlock, item.Key, TimeSpan.Zero);
                        if (dw.IsEmpty)
                            continue;
                        var gu = item.Value;
                        if (OwHelper.ComputeTimeout(gu.LastModifyDateTimeUtc, gu.Timeout) > TimeSpan.Zero) //若尚未超时
                            continue;
                        //准备驱除
                        var cmd = new AccountLogoutingCommand() { User = gu, Reason = GameUserLogoutReason.Timeout };
                        try
                        {
                            svcCommand.Handle(cmd);
                        }
                        catch (Exception excp)
                        {
                            Logger.LogWarning(excp, "用户即将登出事件中产生错误。");
                        }
                        gu.GetDbContext().SaveChanges(true);
                        RemoveUser(item.Key);
                    }
                }
                if (_Lifetime.ApplicationStopping.IsCancellationRequested) break;
                try
                {
                    if (Monitor.TryEnter(_Queue, TimeSpan.FromSeconds(1)))
                    {
                        Monitor.Wait(_Queue, 10000);
                        Monitor.Enter(_Queue);
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
            //准备退出。
        }

        public void RemoveUser(string key)
        {

        }

        /// <summary>
        /// 保存指定key的用户数据。
        /// </summary>
        /// <param name="key">用户的key。</param>
        /// <returns></returns>
        public bool Save(string key)
        {
            var result = _Queue.TryAdd(key, null);
            if (result && Monitor.TryEnter(_Queue, TimeSpan.Zero))
            {
                Monitor.Pulse(_Queue);
                Monitor.Exit(_Queue);
            }
            return result;
        }

        /// <summary>
        /// 通知延迟驱逐。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Nop(string key)
        {
            var gu = _Key2User.GetValueOrDefault(key);
            if (gu is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定的Key={key}代表的用户对象。");
                return false;
            }
            gu.LastModifyDateTimeUtc = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// 使用令牌获取当前登录的角色。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public DisposeHelper<object> GetCharFromToken(Guid token, out GameChar gameChar)
        {
            var key = Token2Key.GetValueOrDefault(token);
            gameChar = null;
            if (key is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_TOKEN);
                return DisposeHelper.Empty<object>();
            }
            var result = DisposeHelper.Create(Lock, Unlock, (object)key, Options.DefaultLockTimeout);
            if (result.IsEmpty)
            {
                OwHelper.SetLastError(ErrorCodes.WAIT_TIMEOUT);
                return result;
            }
            var gu = _Key2User.GetValueOrDefault(key);  //获取用户
            if (gu is null)
            {
                result.Dispose();
                return DisposeHelper.Empty<object>();
            }
            gameChar = gu.CurrentChar;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Lock(object key, TimeSpan timeout) => Options.LockCallback(key, timeout);

        /// <summary>
        /// 使用默认超时设置锁定key。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Lock(object key) => Options.LockCallback(key, Options.DefaultLockTimeout);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void Unlock(object key) => Options.UnlockCallback(key);

        /// <summary>
        /// 加入一个指定的账号对象。
        /// </summary>
        /// <param name="gu"></param>
        /// <returns>true成功加入，false已经存在一个相同Id的对象。</returns>
        /// <exception cref="TimeoutException"></exception>
        public bool AddUser(GameUser gu)
        {
            var key = gu.GetKey();
            using var dw = DisposeHelper.Create(Lock, Unlock, key, TimeSpan.FromSeconds(1));
            if (dw.IsEmpty)
                throw new TimeoutException();
            if (!_LoginName2Key.TryAdd(gu.LoginName, key))
                return false;
            _Token2Key.TryAdd(gu.Token, key);
            _Key2User.TryAdd(key, gu);
            return true;
        }

        /// <summary>
        /// 加入一个指定的角色，若尚未加入其账号，则也自动加入账号对象。
        /// </summary>
        /// <param name="gc"></param>
        /// <exception cref="TimeoutException"></exception>
        public void AddChar(GameChar gc)
        {
            var gcThing = gc.Thing as VirtualThing;
            var guThing = gcThing?.Parent;
            var key = guThing.IdString;
            using var dw = DisposeHelper.Create(Lock, Unlock, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty) throw new TimeoutException();
            if (!_Key2User.ContainsKey(key))
                AddUser(guThing.GetJsonObject<GameUser>());
            _CharId2Key.TryAdd(gcThing.Id, key);
        }

        /// <summary>
        /// 加载或获取已经存在的用户对象。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public bool LoadOrGetUser(string loginName, string pwd, out GameUser user)
        {
            using var dw = DisposeHelper.Create(Lock, Unlock, loginName, Options.DefaultLockTimeout);
            if (dw.IsEmpty) throw new TimeoutException();
            GameUser gu;
            if (_LoginName2Key.TryGetValue(loginName, out string key))    //若内存中找到了指定登录名的账号对象
            {
                using var dwKey = DisposeHelper.Create(Lock, Unlock, key, Options.DefaultLockTimeout);
                if (dwKey.IsEmpty) throw new TimeoutException();
                gu = _Key2User.GetValueOrDefault(key);
                if (!gu.IsPwd(pwd))
                {
                    user = default;
                    return false;
                }
                user = gu;
                return true;
            }
            //加载
            var db = _ContextFactory.CreateDbContext();
            var guThing = db.Set<VirtualThing>().Include(c => c.Children).ThenInclude(c => c.Children).ThenInclude(c => c.Children).ThenInclude(c => c.Children)
                .FirstOrDefault(c => c.ExtraString == loginName);
            if (guThing is null) goto falut;
            gu = guThing.GetJsonObject<GameUser>();
            if (!gu.IsPwd(pwd)) goto falut;
            //设置必要属性

            using (var dwKey = DisposeHelper.Create(Lock, Unlock, guThing.IdString, Options.DefaultLockTimeout))
            {
                if (dwKey.IsEmpty) throw new TimeoutException();
                gu.SetDbContext(db);
                if (gu.Token == Guid.Empty)
                    gu.Token = Guid.NewGuid();
                gu.CurrentChar = guThing.Children.First().GetJsonObject<GameChar>();
                AddUser(gu);
                user = gu;
                return true;
            }
        falut:
            user = null;
            return false;
        }

        #region IDisposable接口相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Token2Key = default;
                _LoginName2Key = default;
                _CharId2Key = default;
                _Key2User = default;
                _ContextFactory = default;
                _Lifetime = default;
                _Service = default;
                _Queue = default;
                base.Dispose(disposing);
            }
        }

        public bool ChangeToken(GameUser gu, Guid guid)
        {
            using var dw = DisposeHelper.Create(Lock, Unlock, gu.GetKey(), TimeSpan.FromSeconds(3));
            if (dw.IsEmpty || gu.IsDisposed)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                return false;
            }
            Token2Key.Remove(gu.Token, out _);
            gu.Token = guid;
            Token2Key.TryAdd(gu.Token, gu.GetKey());
            return true;
        }

        #endregion IDisposable接口相关


    }

}
