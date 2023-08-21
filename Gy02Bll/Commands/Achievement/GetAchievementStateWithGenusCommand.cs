using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    /// <summary>
    /// 按类属获取任务/成就状态命令。
    /// </summary>
    public class GetAchievementStateWithGenusCommand : SyncCommandBase, IGameCharCommand
    {
        public GetAchievementStateWithGenusCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 过滤的类属字符串集合，对于多个元素，任务/成就中只要含其中任一元素就会返回。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// 返回的成就对象。当出错时此集合的状态未知。
        /// </summary>
        public List<GameAchievement> Result { get; set; } = new List<GameAchievement>();
    }

    public class GetAchievementStateWithGenusHandler : SyncCommandHandlerBase<GetAchievementStateWithGenusCommand>, IGameCharHandler<GetAchievementStateWithGenusCommand>
    {
        public GetAchievementStateWithGenusHandler(GameAccountStoreManager accountStore)
        {
            AccountStore = accountStore;
        }

        public GameAccountStoreManager AccountStore { get; }

        public override void Handle(GetAchievementStateWithGenusCommand command)
        {

        }
    }
}
