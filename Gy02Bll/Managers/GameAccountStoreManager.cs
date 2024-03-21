using GY02.Base;
using GY02.Commands;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.GameDb;
using OW.Server;
using OW.SyncCommand;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameAccountStoreManagerOptions : IOptions<GameAccountStoreManagerOptions>
    {
        public GameAccountStoreManagerOptions()
        {

        }

        public GameAccountStoreManagerOptions Value => this;

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
    /// 账号和角色相关的存储及检索索引服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameAccountStoreManager : GameManagerBase<GameAccountStoreManagerOptions, GameAccountStoreManager>
    {
        public GameAccountStoreManager(IOptions<GameAccountStoreManagerOptions> options, ILogger<GameAccountStoreManager> logger, IDbContextFactory<GY02UserContext> contextFactory,
            IHostApplicationLifetime lifetime, IServiceProvider service, GameTemplateManager templateManager, OwScheduler scheduler, GameSqlLoggingManager sqlLoggingManager) : base(options, logger)
        {
            _ContextFactory = contextFactory;
            _Lifetime = lifetime;
            _Service = service;
            _TemplateManager = templateManager;
            _Scheduler = scheduler;

            Task.Factory.StartNew(SaveCallback, TaskCreationOptions.LongRunning);
            _SqlLoggingManager = sqlLoggingManager;
        }

        IDbContextFactory<GY02UserContext> _ContextFactory;
        IHostApplicationLifetime _Lifetime;
        IServiceProvider _Service;
        GameTemplateManager _TemplateManager;
        OwScheduler _Scheduler;
        GameSqlLoggingManager _SqlLoggingManager;

        /// <summary>
        /// 记录所有用户对象。
        /// </summary>
        public ConcurrentDictionary<string, GameUser> _Key2User = new ConcurrentDictionary<string, GameUser>();

        /// <summary>
        /// 记录所有用户对象。
        /// </summary>
        public ConcurrentDictionary<string, GameUser> Key2User => _Key2User;

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
        /// 保存用户信息的幂等操作。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        bool SaveUserCallback(object key, object user)
        {
            if (user is not GameUser gu) return false;
            try
            {
                gu.GetDbContext().SaveChanges();
            }
            catch (DbUpdateConcurrencyException excp)
            {
                var ids = string.Join(',', excp.Entries.Select(c => (c.Entity as VirtualThing)?.IdString));
                var tids = string.Join(',', excp.Entries.Select(c => (c.Entity as VirtualThing)?.ExtraGuid));
                var states = string.Join(',', excp.Entries.Select(c => c?.State));
                Logger.LogWarning(excp, $"保存数据时出现并发错误——ids:{ids}。tids:{tids}。state{states}");
                throw;
            }
            catch (Exception excp)
            {
                Logger.LogWarning(excp, $"保存数据时出错——{excp.Message}。{excp.InnerException?.Message}。");
                throw;
            }

            return true;
        }

        /// <summary>
        /// 后台保存函数。
        /// </summary>
        void SaveCallback()
        {
            while (!_Lifetime.ApplicationStopped.IsCancellationRequested)
            {
                //计算超时
                if (_Key2User.Any())    //若有账号
                {
                    bool isRemoved = false;
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
                        if (_Scheduler.EnsureComplateIdempotent(item.Key, Options.DefaultLockTimeout))  //若保存成功或无保存任务
                        {

                            gu.GetDbContext().SaveChanges(true);
                            if (RemoveUser(item.Key))
                            {
                                ClearUser(gu);
                                isRemoved = true;
                            }
                        }
                    }
                    if (isRemoved) GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                }
                if (_Lifetime.ApplicationStopping.IsCancellationRequested) break;
                _Lifetime.ApplicationStopped.WaitHandle.WaitOne(1);
            }
            //准备退出。
        }

        #region 获取信息

        /// <summary>
        /// 用角色Id，从数据库中读取所属账号的key,key可以用于锁定。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="context">数据库上下文，若省略或为null则自动生成一个临时上下文。</param>
        /// <returns>所属账号的key,key可以用于锁定。null表示出错，此时调用<see cref="OwHelper.GetLastError()"/>获取信息。</returns>
        public string GetKeyByCharId(Guid charId, DbContext context = null)
        {
            bool ownerContext = context is null;
            using var dw = DisposeHelper.Create(c =>
            {
                if (ownerContext) c?.Dispose();
            }, context);
            context ??= _ContextFactory.CreateDbContext();
            var guId = (from gu in context.Set<VirtualThing>().Where(c => c.ExtraGuid == ProjectContent.UserTId)
                        join gc in context.Set<VirtualThing>().Where(c => c.ExtraGuid == ProjectContent.CharTId)
                        on gu.Id equals gc.ParentId
                        where gc.Id == charId
                        select gu.Id).FirstOrDefault();
            if (guId == Guid.Empty)    //若无法获取账号Id。
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"无法找到指定Id的角色或指定的id不是角色，CharId={charId}。");
                return null;
            }
            var key = guId.ToString();
            return key;
        }

        #endregion

        void ClearUser(GameUser user)
        {
            user.GetDbContext()?.Dispose();
            //user.CurrentChar?.Dispose();
            //user.Dispose();
        }

        public bool RemoveUser(string key)
        {
            using var dw = DisposeHelper.Create(Lock, Unlock, key, TimeSpan.Zero);
            if (dw.IsEmpty) return false;
            if (_Key2User.Remove(key, out var gu))
            {
                var gc = gu.CurrentChar;
                _CharId2Key.Remove(gc.Id, out _);
                _Token2Key.Remove(gu.Token, out _);
                _LoginName2Key.Remove(gu.LoginName, out _);
            }
            return true;
        }

        /// <summary>
        /// 保存指定key的用户数据。
        /// </summary>
        /// <param name="key">用户的key。</param>
        /// <returns></returns>
        public bool Save(string key)
        {
            var gu = _Key2User.GetValueOrDefault(key);
            if (gu is null) return false;
            _Scheduler.TryAddIdempotent(key, SaveUserCallback, gu);
            return true;
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
            gu.LastModifyDateTimeUtc = OwHelper.WorldNow;
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

        #region 锁定相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Lock(object key, TimeSpan timeout)
        {
            var result = Options.LockCallback(key, timeout);
            return result;
        }

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
        /// 锁定指定角色所属的账号，但并不试图加载账号和角色。
        /// 首先试图在内存中寻找，以加速性能。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="context">数据库上下文，若省略或为null则自动生成一个临时上下文。</param>
        /// <returns></returns>
        public DisposeHelper<object> LockByCharId(Guid charId, DbContext context = null)
        {
            var key = _CharId2Key.GetValueOrDefault(charId);
            if (key is not null)    //若找到了key
            {
                var result = DisposeHelper.Create(Lock, Unlock, (object)key, Options.DefaultLockTimeout);
                if (!result.IsEmpty)    //若成功锁定
                {
                    if (_Key2User.GetValueOrDefault(key)?.CurrentChar?.Id == charId) //若锁定有效
                        return result;
                    else
                        result.Dispose();   //解锁
                }
            }
            key = GetKeyByCharId(charId, context);
            if (key is null) return DisposeHelper.Empty<object>();
            var result1 = DisposeHelper.Create(Lock, Unlock, (object)key, Options.DefaultLockTimeout);
            return result1;
        }

        #endregion 锁定相关

        /// <summary>
        /// 加入一个指定的账号对象。
        /// </summary>
        /// <param name="gu">指定用户对象，此对象可以没有加载角色，但在加角色后需要调用<see cref="_CharId2Key"/>的方法。</param>
        /// <returns>true成功加入，false已经存在一个相同Id的对象。</returns>
        /// <exception cref="TimeoutException"></exception>
        public bool AddUser(GameUser gu)
        {
            var key = gu.Key;
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

        #region 加载角色或用户

        /// <summary>
        /// 在缓存中获取指定key的账号对象，并锁定后返回。
        /// </summary>
        /// <param name="key">账号的key,</param>
        /// <param name="user">返回的账号对象。</param>
        /// <returns>释放的帮助器。
        /// 若<see cref="DisposeHelper{String}.IsEmpty"/>是true则说明获取成功，此时<paramref name="user"/>是账号对象。false表示失败，此时调用<see cref="OwHelper.GetLastError()"/>可获取详细信息，
        /// <seealso cref="ErrorCodes.WAIT_TIMEOUT"/> 锁定key超时。
        /// <seealso cref="ErrorCodes.ERROR_NO_SUCH_USER"/> 内存中没有指定key的账号对象。
        /// <seealso cref="ErrorCodes.E_CHANGED_STATE"/> 指定key的账号对象在获取时被并发处置。
        /// </returns>
        public virtual DisposeHelper<string> GetUser(string key, out GameUser user)
        {
            var dw = DisposeHelper.Create(Lock, Unlock, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty) //若锁定超时
            {
                user = null;
                return DisposeHelper.Empty<string>();
            }
            try
            {
                user = _Key2User.GetValueOrDefault(key);
                if (user is not null && !user.IsDisposed) return dw;    //若在内存中加载成功
                if (user is null)    //若没有找到
                    OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_NO_SUCH_USER, $"内存中没有指定key的账号对象，Key={key}");
                else //若已并发处置
                    OwHelper.SetLastErrorAndMessage(ErrorCodes.E_CHANGED_STATE, $"指定key的账号对象在获取时被并发处置，Key={key}");
                dw.Dispose();
                return DisposeHelper.Empty<string>();
            }
            catch (Exception)   //放置抛出异常导致错误锁定
            {
                dw.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 从数据库加载一个指定条件的用户账号并返回，但不加入缓存（需要调用者自己加入）。
        /// </summary>
        /// <param name="loadFunc">加载函数，符合该条件的第一个用户对象将被加载并返回。条件会自动附加限定是账号的模板Guid，调用者不必限定。</param>
        /// <param name="context">使用的上下文对象，如果省略或为空引用则自动生成。</param>
        /// <returns>返回找到符合条件的第一账号，否则返回null。</returns>
        public virtual GameUser LoadUser(Expression<Func<VirtualThing, bool>> loadFunc, DbContext context = null)
        {
            //开始从数据库加载
            context ??= _ContextFactory.CreateDbContext();
            var guThing = context.Set<VirtualThing>().Include(c => c.Children).ThenInclude(c => c.Children)/*.ThenInclude(c => c.Children).ThenInclude(c => c.Children)*/
                .Where(c => c.ExtraGuid == ProjectContent.UserTId).FirstOrDefault(loadFunc);
            if (guThing is null)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定条件的账号。");
                return null;
            }
            var user = guThing.GetJsonObject<GameUser>();
            //设置必要属性
            user.SetDbContext(context);
            var tt = _TemplateManager.GetFullViewFromId(ProjectContent.UserTId);
            user.SetTemplate(tt);
            if (user.Token == Guid.Empty) user.Token = Guid.NewGuid();  //设置令牌
            user.CurrentChar = guThing.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.CharTId)?.GetJsonObject<GameChar>();  //设置当前用户
            if (user.CurrentChar is not GameChar gc)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"指定账号没有创建角色,UserKey={user.GetThing().IdString}");
                return null;
            }
#if DEBUG
            foreach (var child in guThing.GetAllChildren())
                child.SetTemplate(_TemplateManager.GetFullViewFromId(child.ExtraGuid));
#endif
            return user;
        }

        /// <summary>
        /// 获取或加载指定的账号。
        /// </summary>
        /// <param name="key">在缓存中找到此标志的用户对象则被直接返回。</param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual DisposeHelper<string> GetOrLoadUser(string key, out GameUser user)
        {
            var dw = GetUser(key, out user);
            if (!dw.IsEmpty) return dw; //若在缓存中找到
            dw = DisposeHelper.Create(Lock, Unlock, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
            {
                user = null;
                return DisposeHelper.Empty<string>();
            }
            try //防止因抛出异常而导致错误的锁定key
            {
                //二次判定
                using var dwUser = GetUser(key, out user);
                if (!dwUser.IsEmpty) return dw; //若在缓存中找到

                //开始从数据库加载
                if (!Guid.TryParse(key, out var userId))
                {
                    dw.Dispose();
                    OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"指定的Key不能转换为用户的唯一Id,Key={key}");
                    return DisposeHelper.Empty<string>();
                }
                user = LoadUser(c => c.Id == userId);
                if (user is null)
                {
                    dw.Dispose();
                    return DisposeHelper.Empty<string>();
                }
                AddUser(user);

                #region 升级购买记录存储方式
                if (user.CurrentChar is GameChar gc)
                {
                    var base64Id = gc.GetThing().Base64IdString;
                    var collShoppingHistory = gc.ShoppingHistory.Select(c =>
                    {
                        var action = new ActionRecord
                        {
                            ActionId = $"{GameShoppingManager.ShoppingBuyHistoryPrefix}.{base64Id}",
                        };
                        var result = GameShoppingHistoryItemV2.From(action);
                        result.TId = c.TId;
                        result.Count = c.Count;
                        result.WorldDateTime = c.DateTime;
                        result.PeriodIndex = c.PeriodIndex;
                        result.Save();
                        return result;
                    }).ToArray();
                    if (collShoppingHistory.Length > 0)
                    {
                        gc.ShoppingHistoryV2.AddRange(collShoppingHistory);
                        _SqlLoggingManager.Save(collShoppingHistory.Select(c => c.ActionRecord));
                        gc.ShoppingHistory.Clear();
                        Save(user.Key);
                    }
                    else //若已经升级
                    {
                        using var db = _SqlLoggingManager.CreateDbContext();
                        var name = $"{GameShoppingManager.ShoppingBuyHistoryPrefix}.{gc.GetThing().Base64IdString}";
                        var coll = db.ActionRecords.Where(c => c.ActionId == name);
                        gc.ShoppingHistoryV2.AddRange(coll.Select(c => GameShoppingHistoryItemV2.From(c)));
                    }
                }
                #endregion 升级购买记录存储方式
                return dw;
            }
            catch (Exception)
            {
                dw.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 加载或获取已经存在的用户对象。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="user"></param>
        /// <returns>锁定凭据，调用者在不再需要锁定时应清理。</returns>
        /// <exception cref="TimeoutException"></exception>
        public DisposeHelper<string> GetOrLoadUser(string loginName, string pwd, out GameUser user)
        {
            using var dwLoginName = DisposeHelper.Create(Lock, Unlock, loginName, Options.DefaultLockTimeout);  //极小概率错序死锁，忽略，当做锁定超时处理
            if (dwLoginName.IsEmpty)
            {
                OwHelper.SetLastError(ErrorCodes.WAIT_TIMEOUT);
                OwHelper.SetLastErrorMessage($"无法锁定用户登录名。");
                user = null;
                return DisposeHelper.Empty<string>();
            }
            //在缓存中查找
            var key = _LoginName2Key.GetValueOrDefault(loginName);
            if (key is not null) //若找到可能的对象
            {
                var dw = GetUser(key, out user);
                if (!dw.IsEmpty)    //若成功找到
                {
                    if (user.LoginName == loginName && user.IsPwd(pwd)) //若密码正确
                        return dw;
                    else //若用户名或密码不正确
                    {
                        dw.Dispose();
                        goto falut;
                    }
                }
            }
            //加载
            var pwdHash = GetPwdHash(pwd);
            using (var db = _ContextFactory.CreateDbContext())
            {
                var id = db.Set<VirtualThing>().Where(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == loginName && c.BinaryArray == pwdHash).Select(c => c.Id).FirstOrDefault();
                if (id == Guid.Empty) goto falut;    //若没有找到指定对象
                key = id.ToString();
                var dw = GetOrLoadUser(key, out user);
                if (dw.IsEmpty) return dw;    //若加载失败
                return dw;
            }
        falut:
            OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_NO_SUCH_USER, $"找不到指定的用户名或密码错误。");
            user = null;
            return DisposeHelper.Empty<string>();
        }

        #endregion 加载角色或用户

        /// <summary>
        /// 获取密码的hash值。
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns>密码的hash值，如果pwd是空引用，则也返回空引用。</returns>
        public byte[] GetPwdHash(string pwd)
        {
            var hash = pwd is null ? null : SHA1.HashData(Encoding.UTF8.GetBytes(pwd));
            return hash;

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
                base.Dispose(disposing);
            }
        }

        public bool ChangeToken(GameUser gu, Guid guid)
        {
            using var dw = DisposeHelper.Create(Lock, Unlock, gu.Key, TimeSpan.FromSeconds(3));
            if (dw.IsEmpty || gu.IsDisposed)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"");
                return false;
            }
            Token2Key.Remove(gu.Token, out _);
            gu.Token = guid;
            Token2Key.TryAdd(gu.Token, gu.Key);
            return true;
        }

        #endregion IDisposable接口相关

    }

}
