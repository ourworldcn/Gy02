using GY02.Managers;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Store;
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
        public AccountCreatedHandler(IServiceProvider serviceProvider, GameEntityManager entityManager)
        {
            _Service = serviceProvider;
            _EntityManager = entityManager;
        }

        IServiceProvider _Service;
        GameEntityManager _EntityManager;

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
            var entities = _EntityManager.GetAllEntity((comm.User.Thing as VirtualThing).Children.FirstOrDefault()?.GetJsonObject<GameChar>());
            _EntityManager.InitializeEntity(entities);
            command.User.CurrentChar = comm.Result;

        }
    }
}
