using Gy02.Publisher;
using Gy02Bll.Managers;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Item
{
    public class GetEntitiesCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        /// <summary>
        /// 
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 需要获取对象的唯一Id集合。
        /// 可以是角色Id,那样将返回角色对象。
        /// </summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();

        /// <summary>
        /// 是否包含子对象。当前版本一律视同为false——都不包含子对象。
        /// </summary>
        public bool IncludeChildren { get; set; }

        /// <summary>
        /// 返回的实体集合。
        /// </summary>
        public List<GameItem> Results { get; set; } = new List<GameItem>();
    }

    public class GetEntitiesHandler : SyncCommandHandlerBase<GetEntitiesCommand>, IGameCharHandler<GetEntitiesCommand>
    {
        public GetEntitiesHandler(GameAccountStore accountStore, GameEntityManager entityManager)
        {
            _AccountStore = accountStore;
            _EntityManager = entityManager;
        }

        GameAccountStore _AccountStore;
        public GameAccountStore AccountStore => _AccountStore;

        GameEntityManager _EntityManager;

        public override void Handle(GetEntitiesCommand command)
        {
            var key = ((IGameCharHandler<GetEntitiesCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<GetEntitiesCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var coll = command.GameChar.GetAllChildren().Select(c => _EntityManager.GetEntity(c)).Append(command.GameChar);

            var resultColl = from tmp in coll
                             join id in command.Ids
                             on tmp.Id equals id
                             select tmp;
            command.Results.AddRange(resultColl.OfType<GameItem>());
            if (command.Results.Count != command.Ids.Count)
            {
                command.Results.Clear();
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"至少有一个唯一对象Id不正确，无法找到对应对象。";
                return;
            }
            return;
        }
    }
}
