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
    public class GameAchievementManagerOptions : IOptions<GameAchievementManagerOptions>
    {
        public GameAchievementManagerOptions Value => this;
    }

    public class GameAchievementManager : GameManagerBase<GameAchievementManagerOptions, GameAchievementManager>
    {
        public GameAchievementManager(IOptions<GameAchievementManagerOptions> options, ILogger<GameAchievementManager> logger) : base(options, logger)
        {
        }
    }
}
