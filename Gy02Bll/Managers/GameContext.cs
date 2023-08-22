using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    /// <summary>
    /// 上下文服务。记录特定于某个数据包相关的一些数据。
    /// 通讯层检测到一个数据包到达，应首先初始化该服务。除非后续调用无需上下文信息。
    /// </summary>
    /// <remarks>
    /// 对于特定的线程而言也的确可以使用本地数据槽来实现，但对特定的数据包并不完全保证在单个线程内实现处理，增加上下文可以增加灵活性。
    /// </remarks>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameContext : OwDisposableBase
    {
        public GameContext(GameAccountStoreManager accountStoreManager)
        {
            AccountStoreManager = accountStoreManager;
        }

        #region 属性及相关

        public Guid Token { get; internal set; }

        public GameAccountStoreManager AccountStoreManager { get; }

        public GameChar GameChar { get; internal set; }

        public Action<object> CharDisposer { get; internal set; }
        public object CharDisposerState { get; internal set; }
        #endregion 属性及相关

        /// <summary>
        /// 给当前上下文设置会话令牌。
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
            return true;
        }

        #region IDisposable接口相关

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
                base.Dispose(disposing);  //        IsDisposed = true;
            }
        }

        #endregion IDisposable接口相关
    }

    public static class GameContextExtensions
    {

    }
}
