using Gy02Bll.Commands.Combat;
using Gy02Bll.Managers;
using MyNamespace;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Fuhua
{
    public class FuhuaPreviewCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 双亲的模板Id集合，无所谓顺序，但返回时是按升序排序。
        /// </summary>
        public List<Guid> ParentTIds { get; set; } = new List<Guid>();
    }

    public class FuhuaPreviewHandler : SyncCommandHandlerBase<FuhuaPreviewCommand>
    {
        public FuhuaPreviewHandler(GameAccountStore gameAccountStore, GameEntityManager gameEntityManager)
        {
            _GameAccountStore = gameAccountStore;
            _GameEntityManager = gameEntityManager;
        }

        GameAccountStore _GameAccountStore;
        GameEntityManager _GameEntityManager;

        public override void Handle(FuhuaPreviewCommand command)
        {
            var key = ((IGameCharHandler<FuhuaPreviewCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<FuhuaPreviewCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            var gc = command.GameChar;

            _GameAccountStore.Save(key);    //保存数据

        }
    }
}
