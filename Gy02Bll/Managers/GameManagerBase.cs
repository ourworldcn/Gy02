using Microsoft.Extensions.Options;
using OW.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Managers
{
    public abstract class GameManagerBase<TOptions> : IDisposable where TOptions : class, IOptions<TOptions>
    {
        #region 构造函数及相关

        protected GameManagerBase()
        {

        }

        protected GameManagerBase(TOptions options)
        {
            Options = options;
        }

        #endregion 构造函数及相关

        private TOptions _Options;
        /// <summary>
        /// 类配置项。
        /// </summary>
        public TOptions Options { get => _Options; protected set => _Options = value; }

        #region IDisposable接口相关

        /// <summary>
        /// 如果对象已经被处置则抛出<see cref="ObjectDisposedException"/>异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DoesNotReturn]
        protected void ThrowIfDisposed()
        {
            if (_IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private bool _IsDisposed;

        protected bool IsDisposed { get => _IsDisposed; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Options = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~LeafMemoryCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关

    }
}
