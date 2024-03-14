using GY02.Managers;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands.Account
{
    public class LoginT0314Command : SyncCommandBase
    {
        /// <summary>
        /// 发行商SDK给的token。
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 发行商给的Uid。
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// 返沪的登录用户对象。
        /// </summary>
        public GameUser User { get; set; }

        /// <summary>
        /// 用户的密码。首次登录才会给出。后续登录是null。
        /// </summary>
        public string Pwd { get; set; }
    }

    public class LoginT0314Handler : SyncCommandHandlerBase<LoginT0314Command>
    {
        public LoginT0314Handler(T0314Manager t0314Manager)
        {
            _T0314Manager = t0314Manager;
        }

        T0314Manager _T0314Manager; 

        public override void Handle(LoginT0314Command command)
        {
            
        }
    }
}
