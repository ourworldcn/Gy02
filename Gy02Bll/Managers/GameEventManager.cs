using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{

    public class GameEventManagerOptions : IOptions<GameEventManagerOptions>
    {
        public GameEventManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏内事件服务类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameEventManager : GameManagerBase<GameEventManagerOptions, GameEventManager>
    {
        public GameEventManager(IOptions<GameEventManagerOptions> options, ILogger<GameEventManager> logger) : base(options, logger)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendEvent(Guid eventTId)
        {

        }
    }
}
