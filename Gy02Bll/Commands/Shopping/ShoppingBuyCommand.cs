
using GY02.Commands;
using GY02.Managers;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Shopping
{
    public class ShoppingBuyCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 购买的商品项Id。
        /// </summary>
        public Guid ShoppingItemTId { get; set; }
    }

    public class ShoppingBuyHandler : SyncCommandHandlerBase<ShoppingBuyCommand>, IGameCharHandler<ShoppingBuyCommand>
    {

        public ShoppingBuyHandler(GameAccountStore accountStore, GameShoppingManager shoppingManager, GameEntityManager entityManager, BlueprintManager blueprintManager)
        {
            AccountStore = accountStore;
            _ShoppingManager = shoppingManager;
            _EntityManager = entityManager;
            _BlueprintManager = blueprintManager;
        }

        public GameAccountStore AccountStore { get; }

        GameEntityManager _EntityManager;
        BlueprintManager _BlueprintManager;
        GameShoppingManager _ShoppingManager;

        public override void Handle(ShoppingBuyCommand command)
        {
            var key = ((IGameCharHandler<ShoppingBuyCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<ShoppingBuyCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            var si = _ShoppingManager.GetShoppingItemByTId(command.ShoppingItemTId);
            if (si is null) goto lbErr;
            var now = DateTime.UtcNow;
            if (!_ShoppingManager.IsValid(command.GameChar, si, now, out _)) goto lbErr;

            var allEntity = _EntityManager.GetAllEntity(command.GameChar)?.ToArray();
            if (allEntity is null) goto lbErr;

            if (!_BlueprintManager.Deplete(allEntity, si.Ins, command.Changes)) goto lbErr;

            if (!_EntityManager.CreateAndMove(si.Outs.Select(c => (c.TId, c.Count, c.ParentTId)), command.GameChar, command.Changes)) goto lbErr;

            AccountStore.Save(key);
            return;
        lbErr:
            command.FillErrorFromWorld();
        }
    }
}

