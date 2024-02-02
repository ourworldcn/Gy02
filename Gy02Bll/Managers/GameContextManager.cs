using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW;
using OW.Game;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    /// <summary>
    /// 针对一个数据包的上下文。通常是一个范围型服务实现此接口。
    /// </summary>
    public interface IGameContext : IDisposable
    {
        /// <summary>
        /// 连接此上下文的Token。
        /// </summary>
        public Guid Token { get; }

        /// <summary>
        /// 该上下文发起的用户。
        /// </summary>
        public GameChar GameChar { get; }

        /// <summary>
        /// 该上下文的创建时间。
        /// </summary>
        public DateTime WorldDateTime { get; }

        /// <summary>
        /// 记录变化的集合。可能是null。表示无需记录。
        /// </summary>
        public ICollection<GamePropertyChangeItem<object>> Changes { get; }
    }

    /// <summary>
    /// 一个简单实现，仅用于封装传送数据目的。
    /// </summary>
    public class SimpleGameContext : IGameContext
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="gameChar"></param>
        /// <param name="worldNow"></param>
        /// <param name="changes"></param>
        public SimpleGameContext(Guid token, GameChar gameChar, DateTime worldNow, ICollection<GamePropertyChangeItem<object>> changes)
        {
            Token = token;
            GameChar = gameChar;
            WorldDateTime = worldNow;
            Changes = changes;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Guid Token { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameChar GameChar { get; internal set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime WorldDateTime { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ICollection<GamePropertyChangeItem<object>> Changes { get; internal set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Dispose()
        {
            GameChar = default;
            Changes = default;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 模拟上下文。
    /// </summary>
    public interface IImpersonationGameContext : IGameContext
    {

    }

    /// <summary>
    /// 上下文服务。记录特定于某个工作单元(数据包)相关的一些数据。这个服务是范围性的。
    /// 通讯层检测到一个数据包到达，应首先初始化该服务。除非后续调用无需上下文信息。
    /// </summary>
    /// <remarks>
    /// 对于特定的线程而言也的确可以使用本地数据槽来实现，但对特定的数据包并不完全保证在单个线程内实现处理，增加上下文可以增加灵活性。
    /// </remarks>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameContextManager : OwDisposableBase, IGameContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopeServiceProvider"></param>
        /// <param name="accountStoreManager"></param>
        /// <param name="logger"></param>
        public GameContextManager(IServiceProvider scopeServiceProvider, GameAccountStoreManager accountStoreManager, ILogger<GameContextManager> logger, SyncCommandManager commandManager)
        {
            AccountStoreManager = accountStoreManager;
            ScopeServiceProvider = scopeServiceProvider;
            Logger = logger;
            _CommandManager = commandManager;
        }

        #region 属性及相关

        /// <summary>
        /// 范围服务容器。尽量不使用该属性，只有在必须获取非单例模式的服务时才使用该属性。
        /// </summary>
        public IServiceProvider ScopeServiceProvider { get; internal set; }

        /// <summary>
        /// 使用的日志服务。
        /// </summary>
        public ILogger<GameContextManager> Logger { get; internal set; }

        public GameAccountStoreManager AccountStoreManager { get; }

        SyncCommandManager _CommandManager;

        /// <summary>
        /// 连接此上下文的Token。
        /// </summary>
        public Guid Token { get; internal set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameChar GameChar { get; internal set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime WorldDateTime { get; } = OwHelper.WorldNow;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ICollection<GamePropertyChangeItem<object>> Changes { get; set; }

        /// <summary>
        /// 解锁的回调。
        /// </summary>
        public Action<object> CharDisposer { get; internal set; }

        /// <summary>
        /// 解锁回调所需参数。
        /// </summary>
        public object CharDisposerState { get; internal set; }

        #endregion 属性及相关

        /// <summary>
        /// 给当前上下文设置会话令牌。并锁定令牌代表的用户。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <returns>true成功获得访问全，false未能成功，此时调用<seealso cref="OwHelper.GetLastError()"/>确定具体问题。</returns>
        public bool SetTokenAndLock(Guid token)
        {
            Token = token;
            var dw = AccountStoreManager.GetCharFromToken(token, out var gc);
            if (dw.IsEmpty)
            {
                return false;
            }
            CharDisposer = dw.Action;
            CharDisposerState = dw.State;
            GameChar = gc;
            return true;
        }

        /// <summary>
        /// 当前上下文是否已经初始化角色对象并锁定。
        /// </summary>
        /// <returns>true已经正确初始化，否则返回false。</returns>
        public bool Validate()
        {
            //var r = new ValidationResult("") { };
            if (GameChar is null || CharDisposerState is null) return false;
            if (!AccountStoreManager.Options.IsEnteredCallback(CharDisposerState)) return false;
            return true;
        }

        /// <summary>
        /// 调用命令处理程序，且在之前和之后调用命令处理预览和后处理程序。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        public void Handle<T>(T command) where T : ISyncCommand
        {
            _CommandManager.Handle(command);
        }

        #region IDisposable接口相关

        /// <summary>
        /// 调用此实现进行必要的清理操作。
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                    CharDisposer?.Invoke(CharDisposerState);
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                CharDisposer = null;
                CharDisposerState = null;
                GameChar = null;
                base.Dispose(disposing);  //        IsDisposed = true;
            }
        }

        #endregion IDisposable接口相关
    }

    public static class GameContextExtensions
    {

    }
}
