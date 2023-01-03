using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Manager
{
    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameCharManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        public GameCharManager(IServiceProvider service)
        {
            Service = service;
        }

        /// <summary>
        /// 获取或设置服务容器。
        /// </summary>
        public IServiceProvider Service { get; set; }

        ConcurrentDictionary<Guid, GameChar> _Id2Char;

        ConcurrentDictionary<string, GameChar> _LoginName2Char;
    }
}
