using Gy02Bll.Managers;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Account
{
    public class LoginCommand : SyncCommandBase
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

    public class LoginHandler : SyncCommandHandlerBase<LoginCommand>
    {

        public LoginHandler(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        public override void Handle(LoginCommand command)
        {
            var svcStore = _Service.GetRequiredService<GameAccountStore>();
            var exists = svcStore.LoginName2Key.ContainsKey(command.LoginName);  //是否已经登录
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
            //设置属性
            gu.Timeout = TimeSpan.FromMinutes(15);
            gu.LastModifyDateTimeUtc = DateTime.UtcNow;
            if (exists)  //若是重新登录
            {
                if (!svcStore.ChangeToken(gu, Guid.NewGuid()))
                {
                    command.FillErrorFromWorld();
                    return;
                }
            }
            var db = gu.GetDbContext();
            gu.CurrentChar = ((VirtualThing)gu.Thing).Children.First().GetJsonObject<GameChar>();
        }
    }
}
