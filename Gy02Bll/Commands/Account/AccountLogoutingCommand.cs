using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Account
{
    /// <summary>
    /// 用户注销(登出)的原因。
    /// </summary>
    public enum GameUserLogoutReason
    {
        /// <summary>
        /// 超时。
        /// </summary>
        Timeout,

        /// <summary>
        /// 用户要求。
        /// </summary>
        UserAction,

        /// <summary>
        /// 系统强制。
        /// </summary>
        System,
    }

    /// <summary>
    /// 用户即将登出的事件数据类。
    /// </summary>
    public class AccountLogoutingCommand : SyncCommandBase
    {
        public GameUser User { get; set; }

        public GameUserLogoutReason Reason { get; set; }
    }

    public class AccountLogoutingHandler : SyncCommandHandlerBase<AccountLogoutingCommand>
    {
        public override void Handle(AccountLogoutingCommand command)
        {

        }
    }
}
