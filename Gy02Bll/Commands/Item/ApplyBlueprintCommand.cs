using GY02.Commands;
using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.SyncCommand;

namespace GY02.Commands
{
    /// <summary>
    /// 应用蓝图的命令
    /// </summary>
    public class ApplyBlueprintCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public ApplyBlueprintCommand()
        {

        }

        /// <summary>
        /// 针对的角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 指定使用的输入材料。
        /// </summary>
        public List<GameEntity> InItems { get; set; } = new List<GameEntity>();

        /// <summary>
        /// 完成蓝图变换后输出的物品。
        /// </summary>
        public List<GameEntity> OutItems { get; set; } = new List<GameEntity>();

        /// <summary>
        /// 指定的蓝图。
        /// </summary>
        public TemplateStringFullView Blueprint { get; set; }

    }

    public class ApplyBlueprintHandler : SyncCommandHandlerBase<ApplyBlueprintCommand>, IGameCharHandler<ApplyBlueprintCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="gameAccountStore"></param>
        public ApplyBlueprintHandler(GameAccountStoreManager gameAccountStore, GameBlueprintManager blueprintManager, SyncCommandManager syncCommandManager, GameTemplateManager templateManager, GameEntityManager gameEntityManager, VirtualThingManager virtualThingManager)
        {
            _GameAccountStore = gameAccountStore;
            _BlueprintManager = blueprintManager;
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
            _GameEntityManager = gameEntityManager;
            _VirtualThingManager = virtualThingManager;
        }

        GameAccountStoreManager _GameAccountStore;
        GameBlueprintManager _BlueprintManager;
        SyncCommandManager _SyncCommandManager;
        GameTemplateManager _TemplateManager;
        GameEntityManager _GameEntityManager;
        VirtualThingManager _VirtualThingManager;

        public GameAccountStoreManager AccountStore => _GameAccountStore;

        public override void Handle(ApplyBlueprintCommand command)
        {
            var key = ((IGameCharHandler<ApplyBlueprintCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<ApplyBlueprintCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            var bp = command.Blueprint;
            //TODO 校验材料是否符合要求
            //if (!(bp?.In?.Count > 0 && bp?.Out?.Count > 0))
            //{
            //    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    command.DebugMessage = "指定蓝图Id不正确。";
            //    return;
            //}
            //if (!command.InItems.All(item => bp.In.Any(c => _BlueprintManager.IsMatch(item, c))))
            //{
            //    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    command.DebugMessage = "至少一个材料不符合蓝图输入项的要求。";
            //    return;
            //}
            //生成输出项
            List<GameEntity> outs = new List<GameEntity>();
            var creates = _GameEntityManager.Create(bp.Out);
            if (creates is null)
            {
                command.FillErrorFromWorld();
                return;
            }
            outs.AddRange(creates.Select(c => c.Item2));
            //消耗材料
            //foreach (var item in command.InItems)
            //{
            //    _GameEntityManager.Modify(item, -item.Count, command.Changes);
            //}
            //_GameEntityManager.Move(outs, command.GameChar, command.Changes);
            command.OutItems.AddRange(outs);
            _GameAccountStore.Save(key);
        }
    }
}
