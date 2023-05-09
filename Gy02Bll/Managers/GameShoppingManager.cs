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
    /// 商城管理器的配置类。
    /// </summary>
    public class GameShoppingManagerOptions : IOptions<GameShoppingManagerOptions>
    {
        public GameShoppingManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏商城管理器。
    /// </summary>
    public class GameShoppingManager : GameManagerBase<GameShoppingManagerOptions, GameShoppingManager>
    {
        public GameShoppingManager(IOptions<GameShoppingManagerOptions> options, ILogger<GameShoppingManager> logger) : base(options, logger)
        {
        }

    }
}
