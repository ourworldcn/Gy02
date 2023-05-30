using GY02.Templates;
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
    public class GameMailManagerOptions : IOptions<GameMailManagerOptions>
    {
        public GameMailManagerOptions()
        {
            
        }

        public GameMailManagerOptions Value => this;
    }

    /// <summary>
    /// 管理邮件的服务。
    /// </summary>
    public class GameMailManager : GameManagerBase<GameMailManagerOptions, GameMailManager>
    {
        public GameMailManager(IOptions<GameMailManagerOptions> options, ILogger<GameMailManager> logger) : base(options, logger)
        {
        }
    }
}
