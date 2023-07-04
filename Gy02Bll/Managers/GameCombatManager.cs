using GY02.Templates;
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
        public GameCombatManager(IOptions<GameCombatManagerOptions> options, ILogger<GameCombatManager> logger, GameTemplateManager templateManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
        }

        GameTemplateManager _TemplateManager;

        public TemplateStringFullView GetTemplateById(Guid tId)
        {
            var tt = _TemplateManager.GetFullViewFromId(tId);
            return tt;
        }
    }
}
