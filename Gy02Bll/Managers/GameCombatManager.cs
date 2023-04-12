using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    /// <summary>
    /// 战斗管理器的配置选项。
    /// </summary>
    public class GameCombatManagerOptions : IOptions<GameCombatManagerOptions>
    {
        public GameCombatManagerOptions Value => this;
    }

    /// <summary>
    /// 战斗管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameCombatManager : GameManagerBase<GameCombatManagerOptions, GameCombatManager>
    {
        public GameCombatManager(IOptions<GameCombatManagerOptions> options, ILogger<GameCombatManager> logger) : base(options, logger)
        {
        }
    }
}
