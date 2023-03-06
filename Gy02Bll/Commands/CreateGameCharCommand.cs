using Gy02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    /// <summary>
    /// 创建角色的命令。
    /// </summary>
    public class CreateGameCharCommand : GameCommandBase
    {
        public CreateGameCharCommand() { }

        /// <summary>
        /// 要在此账号下创建角色。
        /// </summary>
        public GameUser User { get; set; }

        /// <summary>
        /// 角色的显示名称，需要唯一。
        /// 若是空引用则自动生成一个新名字。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 返回创建的角色对象。
        /// </summary>
        public GameChar Result { get; set; }
    }

    /// <summary>
    /// 创建角色的命令处理器。
    /// </summary>
    public class CreateGameCharHandler : GameCommandHandlerBase<CreateGameCharCommand>
    {
        public CreateGameCharHandler(IServiceProvider service)
        {
            _Service = service;
        }

        /// <summary>
        /// 范围服务容器。
        /// </summary>
        IServiceProvider _Service;

        public override void Handle(CreateGameCharCommand command)
        {
            using var dw = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, command.User.Id.ToString(), TimeSpan.FromSeconds(1));
            if (dw.IsEmpty)
            {
                command.ErrorCode = 258;
                return;
            }
            var commandCvt = new CreateVirtualThingCommand()
            {
                TemplateId = ProjectContent.CharTId,
            };
            var gcm = _Service.GetRequiredService<GameCommandManager>();
            gcm.Handle(commandCvt);
            if (commandCvt.HasError)
            {
                command.FillErrorFrom(commandCvt);
                return;
            }
            var result = commandCvt.Result;
            var gc = result.GetJsonObject<GameChar>();
            gc.UserId = command.User.Id;
            var db = command.User.GetDbContext();
            db.Add(result);
            command.Result = gc;
        }
    }
}
