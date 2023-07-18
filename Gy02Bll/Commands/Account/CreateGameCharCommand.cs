using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;

namespace GY02.Commands
{
    /// <summary>
    /// 创建角色的命令。
    /// </summary>
    public class CreateGameCharCommand : SyncCommandBase
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
    public class CreateGameCharHandler : SyncCommandHandlerBase<CreateGameCharCommand>
    {
        public CreateGameCharHandler(IServiceProvider service, GameEntityManager gameEntityManager)
        {
            _Service = service;
            _GameEntityManager = gameEntityManager;
        }

        /// <summary>
        /// 范围服务容器。
        /// </summary>
        IServiceProvider _Service;
        GameEntityManager _GameEntityManager;

        public override void Handle(CreateGameCharCommand command)
        {
            var key = command.User.Id.ToString();
            var svcStore = _Service.GetRequiredService<GameAccountStoreManager>();
            using var dw = DisposeHelper.Create(svcStore.Lock, svcStore.Unlock, key, TimeSpan.FromSeconds(1));
            if (dw.IsEmpty)
            {
                command.ErrorCode = ErrorCodes.WAIT_TIMEOUT;
                return;
            }
            var commandCvt = new CreateVirtualThingsCommand() { };
            commandCvt.TIds.Add(ProjectContent.CharTId);
            //{
            //    TemplateId = ProjectContent.CharTId,
            //};
            var gcm = _Service.GetRequiredService<SyncCommandManager>();
            gcm.Handle(commandCvt);
            if (commandCvt.HasError)
            {
                command.FillErrorFrom(commandCvt);
                return;
            }
            var result = commandCvt.Result.First();
            //设置角色的属性
            var gc = result.GetJsonObject<GameChar>();
            gc.UserId = command.User.Id;
            result.ExtraGuid = ProjectContent.CharTId;
            result.Parent = command.User.Thing as VirtualThing;
            ((VirtualThing)command.User.Thing).Children.Add(result);

            _GameEntityManager.GetAllEntity(gc);
            var coll = gc.GetAllChildren().Select(c =>
            {
                var entity = _GameEntityManager.GetEntity(c);
                var tt = _GameEntityManager.GetTemplate(entity);
                return (entity, tt);
            }).Where(c => !c.tt.IsStk()).ToArray();
            coll.SafeForEach(c => c.entity.Count = 1);
            svcStore.AddChar(gc);   //加入缓存

            command.Result = gc;
            svcStore.Save(key);
        }
    }
}
