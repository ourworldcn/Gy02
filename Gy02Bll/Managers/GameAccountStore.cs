using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using OW.Game.Store;
using OW.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    public class GameAccountStoreOptions : IOptions<GameAccountStoreOptions>
    {
        public GameAccountStoreOptions()
        {

        }

        public GameAccountStoreOptions Value => this;
    }

    /// <summary>
    /// 存储及索引服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameAccountStore : GameManagerBase<GameAccountStoreOptions, GameAccountStore>, IDisposable
    {
        public GameAccountStore(IOptions<GameAccountStoreOptions> options, OwServerMemoryCache cache,
            ILogger<GameAccountStore> logger) : base(options, logger)
        {
            _Cache = cache;
        }

        /// <summary>
        /// 底层存储的缓存对象。
        /// </summary>
        OwServerMemoryCache _Cache;

        public OwServerMemoryCache Cache { get => _Cache; }

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
                _Cache = default;
                _Token2Key = default;
                _LoginName2Key = default;
                _CharId2Key = default;
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable接口相关


    }

}
