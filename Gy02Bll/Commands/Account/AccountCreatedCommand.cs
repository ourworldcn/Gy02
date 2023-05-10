using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.SyncCommand;

namespace GY02.Commands
{
    /// <summary>
    /// 账号已经被创建的事件数据。
    /// </summary>
    public class AccountCreatedCommand : SyncCommandBase
    {
        public AccountCreatedCommand() { }

        public GameUser User { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AccountCreatedHandler : SyncCommandHandlerBase<AccountCreatedCommand>
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
            _Service.GetRequiredService<SyncCommandManager>().Handle(comm);

            if (comm.HasError)
            {
                command.FillErrorFrom(comm);
                return;
            }
            command.User.CurrentChar = comm.Result;
        }
    }
}
