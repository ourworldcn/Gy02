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
    public class GameEntityManagerOptions : IOptions<GameEntityManagerOptions>
    {
        public GameEntityManagerOptions Value => this;
    }

    public class GameEntityManager : GameManagerBase<GameEntityManagerOptions, GameEntityManager>
    {
        public GameEntityManager(IOptions<GameEntityManagerOptions> options, ILogger<GameEntityManager> logger) : base(options, logger)
        {
        }
    }
}
