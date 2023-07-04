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

    public class GetAchievementStateCommand : SyncCommandBase, IGameCharCommand
    {
        public GetAchievementStateCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 指定获取成就对象的模板Id集合。
        /// </summary>
        public List<Guid> TIds { get; set; } = new List<Guid>();

        #region 返回数据

        /// <summary>
        /// 返回的成就对象。当出错时此集合的状态未知。
        /// </summary>
        public List<GameAchievement> Result { get; set; } = new List<GameAchievement>();

        #endregion 返回数据
    }

    public class GetAchievementStateHandler : SyncCommandHandlerBase<GetAchievementStateCommand>, IGameCharHandler<GetAchievementStateCommand>
    {
        public GetAchievementStateHandler(GameAccountStoreManager accountStore, GameAchievementManager achievementManager)
        {
            AccountStore = accountStore;
            _AchievementManager = achievementManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameAchievementManager _AchievementManager;

        public override void Handle(GetAchievementStateCommand command)
        {
            var key = ((IGameCharHandler<GetAchievementStateCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<GetAchievementStateCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            foreach (var item in command.TIds)
            {
                var tt = _AchievementManager.GetAchievementById(item);
                if (tt is null) goto lbErr;
                var achi = _AchievementManager.GetOrCreate(command.GameChar, tt);
                if (achi is null) goto lbErr;
                if (!_AchievementManager.RefreshState(achi)) goto lbErr;
                command.Result.Add(achi);
            }
            return;
        lbErr:
            command.FillErrorFromWorld();
            return;
        }
    }
}
