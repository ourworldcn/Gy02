using GY02.Managers;
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
    /// 获取成就奖励功能。
    /// </summary>
    public class GetAchievementRewardsCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GetAchievementRewardsCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 指定获取成就对象的模板Id集合。
        /// </summary>
        public List<Guid> TIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 获取指定成就的指定等级的奖励，此集合顺序与 TIds 集合顺序一致。
        /// </summary>
        public List<int[]> Levels { get; set; } = new List<int[]>();
    }

    public class GetAchievementRewardsHandler : SyncCommandHandlerBase<GetAchievementRewardsCommand>, IGameCharHandler<GetAchievementRewardsCommand>
    {
        public GetAchievementRewardsHandler(GameAccountStoreManager accountStore, GameAchievementManager achievementManager)
        {
            AccountStore = accountStore;
            _AchievementManager = achievementManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameAchievementManager _AchievementManager;

        public override void Handle(GetAchievementRewardsCommand command)
        {
            var key = ((IGameCharHandler<GetAchievementRewardsCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<GetAchievementRewardsCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            for (int i = 0; i < command.TIds.Count; i++)
            {
                var item = command.TIds[i];
                var tt = _AchievementManager.GetTemplateById(item);
                if (tt is null) goto lbErr;
                var b = _AchievementManager.GetRewards(command.GameChar, tt, command.Levels[i], command.Changes);
                if (!b) goto lbErr;
            }
            AccountStore.Save(key);
            return;
        lbErr:
            command.FillErrorFromWorld();
            return;
        }
    }
}
