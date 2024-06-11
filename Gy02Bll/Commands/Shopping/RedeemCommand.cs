using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class RedeemCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 兑换码。
        /// </summary>
        public string Code { get; set; }
    }

    /// <summary>
    /// 使用兑换码兑换。
    /// </summary>
    public class RedeemCommandHandler : SyncCommandHandlerBase<RedeemCommand>, IGameCharHandler<RedeemCommand>
    {
        public RedeemCommandHandler(GameAccountStoreManager accountStore, GY02UserContext dbContext, SyncCommandManager syncCommand)
        {
            AccountStore = accountStore;
            _DbContext = dbContext;
            _syncCommand = syncCommand;
        }

        public GameAccountStoreManager AccountStore { get; }
        GY02UserContext _DbContext;
        SyncCommandManager _syncCommand;

        public override void Handle(RedeemCommand command)
        {
            var redeem = _DbContext.GameRedeemCodes.Find(command.Code);
            if (redeem is null)  //若指定兑换码无效
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            var catalog = _DbContext.GameRedeemCodeCatalogs.FirstOrDefault(c => c.Id == redeem.CatalogId);
            if (catalog is null)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "无效的兑换码";
                return;
            }
            if (catalog.CodeType == 2 && redeem.Count > 0) //若已经兑换
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = "不可重复兑换";
                return;
            }
            var tid = catalog.ShoppingTId;
            var shopping = new ShoppingBuyCommand
            {
                GameChar = command.GameChar,
                ShoppingItemTId = tid,
                Count = 1,
                Changes = command.Changes,
            };
            _syncCommand.Handle(shopping);
            if (shopping.HasError)
            {
                command.FillErrorFrom(shopping);
                command.ErrorCode = 1219;
            }
            else
            {
                redeem.Count++;
                _DbContext.SaveChanges();
            }
        }
    }
}
