using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;

namespace GY02.Commands
{
    /// <summary>
    /// 合成功能命令。
    /// </summary>
    public class CompositeCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CompositeCommand() { }

        /// <summary>
        /// 指定的蓝图。
        /// </summary>
        public TemplateStringFullView Blueprint { get; set; }

        /// <summary>
        /// 指定主要材料。
        /// </summary>
        public GameEntity MainItem { get; set; }

        /// <summary>
        /// 是否恢复主要材料的等级到主要输出物品上。
        /// </summary>
        public bool RestoreLevel { get; set; }

        /// <summary>
        /// 指定使用的辅助材料。
        /// </summary>
        public List<GameEntity> Items { get; set; } = new List<GameEntity>();

        /// <summary>
        /// 针对的角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }
    }

    public class CompositeHandler : SyncCommandHandlerBase<CompositeCommand>
    {
        public CompositeHandler(GameTemplateManager templateManager, GameAccountStoreManager gameAccountStore, GameBlueprintManager blueprintManager, GameEntityManager gameEntityManager, SyncCommandManager syncCommandManager)
        {
            _TemplateManager = templateManager;
            _GameAccountStore = gameAccountStore;
            _BlueprintManager = blueprintManager;
            _GameEntityManager = gameEntityManager;
            _SyncCommandManager = syncCommandManager;
        }

        GameTemplateManager _TemplateManager;
        GameAccountStoreManager _GameAccountStore;
        GameBlueprintManager _BlueprintManager;
        GameEntityManager _GameEntityManager;
        SyncCommandManager _SyncCommandManager;

        /// <summary>
        /// 指定材料是否有已穿戴装备。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="container">返回第一个符合要求的容器。</param>
        /// <returns>true任何一个装备是已穿戴的，则返回true,如果没有任何材料或任何材料都不是已经穿戴的装备则返回false。</returns>
        bool IsEqu(IEnumerable<GameEntity> entities, out GameEntity container)
        {
            var entity = entities.FirstOrDefault(c =>
            {
                if (_GameEntityManager.GetParent(c) is not GameEntity entity) return false;
                if (_TemplateManager.GetFullViewFromId(entity.TemplateId) is not TemplateStringFullView tt) return false;
                return tt.Gid / 1000 == 912111;
            });
            if (entity == null)
            {
                container = null;
                return false;
            }
            container = _GameEntityManager.GetParent(entity);
            return true;
        }

        public override void Handle(CompositeCommand command)
        {
            var key = command.GameChar.GetUser().Key;
            using var dw = DisposeHelper.Create(_GameAccountStore.Lock, _GameAccountStore.Unlock, key, TimeSpan.FromSeconds(2));
            if (dw.IsEmpty)
            {
                command.FillErrorFromWorld();
                return;
            }
            var bp = command.Blueprint;
            //TODO 检测参数合法性
            //if ((bp?.In?.Count ?? 0) <= 0 || (bp?.Out?.Count ?? 0) <= 0)
            //{
            //    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    command.DebugMessage = "指定蓝图Id不正确。";
            //    return;

            //}
            //if (!bp.In.Any(c => _BlueprintManager.IsMatch(command.MainItem, c)))
            //{
            //    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    command.DebugMessage = "主材料不符合蓝图输入项的要求。";
            //    return;
            //}
            //if (!command.Items.All(item => bp.In.Any(c => _BlueprintManager.IsMatch(item, c))))
            //{
            //    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    command.DebugMessage = "至少一个材料不符合蓝图输入项的要求。";
            //    return;
            //}

            var oldLv = command.MainItem.Level; //主要材料的等级
            var cost = command.Items.Append(command.MainItem).Select(c => new GameEntitySummary
            {
                TId = c.TemplateId,
                Count = c.Count,
                Id = c.Id,
                ParentTId = _GameEntityManager.GetParentThing(c)?.ExtraGuid
            }).ToArray(); //记录消耗材料
            //应用蓝图
            var commandApply = new ApplyBlueprintCommand
            {
                Blueprint = command.Blueprint,
                GameChar = command.GameChar,
            };
            commandApply.InItems.AddRange(command.Items);
            commandApply.InItems.Add(command.MainItem);

            _SyncCommandManager.Handle(commandApply);
            if (command.HasError)
            {
                command.FillErrorFrom(commandApply);
                return;
            }
            var isEqu = IsEqu(command.Items.Append(command.MainItem), out var container);  //获取特定父容器
            //消耗代价
            var removes = command.Items.Append(command.MainItem).Select(c => (Entity: c, Count: -c.Count)).ToArray();
            foreach (var item in removes)
            {
                //TODO 要处理不是完整消耗的情况
                if (item.Entity.LvUpAccruedCost?.Count > 0) //若需要降低等级
                {
                    var lvDown = new LvDownCommand { Entity = item.Entity, GameChar = command.GameChar };
                    _SyncCommandManager.Handle(lvDown);
                    command.Changes.AddRange(lvDown.Changes);
                }

                _GameEntityManager.Modify(item.Entity, item.Count, command.Changes);
            }
            //生成物品
            var mainOut = commandApply.OutItems.First(); //主要输出物
            if (isEqu)   //若有装备中的物品
                _GameEntityManager.Move(commandApply.OutItems, container, command.Changes);
            else
                _GameEntityManager.Move(commandApply.OutItems, command.GameChar, command.Changes);

            //生成合成材料记录
            var oldCost = mainOut.CompositingAccruedCost?.ToArray() ?? Array.Empty<GameEntitySummary>().ToArray();    //旧的合成材料记录
            var newCost = (from tmp in oldCost.Concat(cost)   //新材料记录
                           group tmp by tmp.TId into g
                           select new GameEntitySummary { TId = g.Key, Count = g.Sum(x => x.Count) }).ToArray();
            //command.Changes?.Add(new GamePropertyChangeItem<object>
            //{
            //    Object = mainOut,
            //    PropertyName = nameof(mainOut.CompositingAccruedCost),

            //    HasOldValue = oldCost.Length > 0,
            //    OldValue = oldCost,

            //    HasNewValue = newCost.Length > 0,
            //    NewValue = newCost,
            //});
            mainOut.CompositingAccruedCost.Clear();
            //TODO 暂时屏蔽
            //mainOut.CompositingAccruedCost.AddRange(newCost);
            //恢复主材料等级
            if (command.RestoreLevel)
            {
                LvUpCommand lvupCommand;
                for (int i = 0; i < oldLv; i++)
                {
                    lvupCommand = new LvUpCommand { GameChar = command.GameChar, };
                    lvupCommand.Ids.Add(mainOut.Id);
                    _SyncCommandManager.Handle(lvupCommand);
                    if (lvupCommand.HasError)
                    {
                        command.FillErrorFrom(lvupCommand);
                        return;
                    }
                    else
                        command.Changes.AddRange(lvupCommand.Changes);
                }
            }
            _GameAccountStore.Save(key);
        }
    }
}
