using Gy02Bll.Managers;
using Microsoft.Extensions.DependencyInjection;
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

        public LoginHandler(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        public override void Handle(LoginCommand command)
        {
            var svcStore = _Service.GetRequiredService<GameAccountStore>();
            if (!svcStore.LoadOrGetUser(command.LoginName, command.Pwd, out var gu))
            {
                command.FillErrorFromWorld();
                return;
            }
            using var dw = DisposeHelper.Create(svcStore.Lock, svcStore.Unlock, gu.GetKey(), TimeSpan.FromSeconds(3));
            if (dw.IsEmpty || gu.IsDisposed)
            {
                command.FillErrorFromWorld();
                return;
            }
            command.User = gu;

            gu.Timeout = TimeSpan.FromMinutes(15);
            gu.LastModifyDateTimeUtc= DateTime.UtcNow;
        }
    }
}
