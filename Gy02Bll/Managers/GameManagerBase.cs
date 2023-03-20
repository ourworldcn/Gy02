using Microsoft.Extensions.Logging;
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
    public abstract class GameManagerBase<TOptions, TService> : OwServiceBase<TOptions, TService> where TOptions : class
    {
        #region 构造函数及相关

        protected GameManagerBase(IOptions<TOptions> options, ILogger<TService> logger) : base(options, logger)
        {
        }

        #endregion 构造函数及相关

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
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable接口相关

    }
}
