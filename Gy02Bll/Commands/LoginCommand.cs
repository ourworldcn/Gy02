using OW.Game;
using OW.Game.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class LoginCommand : GameCommandBase
    {
        public LoginCommand()
        {

        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }

        #endregion 可映射属性

        public GameUser User { get; set; }
    }

    public class LoginHandler : GameCommandHandlerBase<LoginCommand>
    {
        public override void Handle(LoginCommand command)
        {
            command.ErrorCode = 258;
        }
    }
}
