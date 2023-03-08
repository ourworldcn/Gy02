using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    /// <summary>
    /// 账号已经被创建的事件数据。
    /// </summary>
    public class AccountCreatedCommand : GameCommandBase
    {
        public AccountCreatedCommand() { }

        public GameUser User { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AccountCreatedHandler : GameCommandHandlerBase<AccountCreatedCommand>
    {
        public AccountCreatedHandler(IServiceProvider serviceProvider)
        {
            _Service = serviceProvider;
        }

        IServiceProvider _Service;

        public override void Handle(AccountCreatedCommand command)
        {
            //创建角色
            var comm = new CreateGameCharCommand()
            {
                DisplayName = command.User.LoginName,
                User = command.User,
            };
            _Service.GetRequiredService<GameCommandManager>().Handle(comm);

            if (comm.HasError)
            {
                command.FillErrorFrom(comm);
                return;
            }
            command.User.SetCurrentChar(comm.Result);
        }
    }
}
