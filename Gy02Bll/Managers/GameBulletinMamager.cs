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
    public class GameBulletinMamagerOptions : IOptions<GameBulletinMamagerOptions>
    {
        public GameBulletinMamagerOptions Value => this;
    }

    /// <summary>
    /// 公告管理器。
    /// </summary>
    public class GameBulletinMamager : GameManagerBase<GameBulletinMamagerOptions, GameBulletinMamager>
    {
        public GameBulletinMamager(IOptions<GameBulletinMamagerOptions> options, ILogger<GameBulletinMamager> logger) : base(options, logger)
        {
        }


    }
}
