﻿using GY02.Managers;
using GY02.Commands;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;

namespace GY02.Commands
{
    /// <summary>
    /// 自动合成紫色（不含）以下装备。
    /// </summary>
    public class AutoCompositeCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }
    }

    public class AutoCompositeHandler : SyncCommandHandlerBase<AutoCompositeCommand>, IGameCharHandler<AutoCompositeCommand>
    {
        public AutoCompositeHandler(SyncCommandManager syncCommandManager, GameTemplateManager templateManager, GameEntityManager entityManager, GameAccountStoreManager accountStore)
        {
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
            _AccountStore = accountStore;
        }

        SyncCommandManager _SyncCommandManager;
        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;
        GameAccountStoreManager _AccountStore;

        public GameAccountStoreManager AccountStore => _AccountStore;

        public override void Handle(AutoCompositeCommand command)
        {
            var key = ((IGameCharHandler<AutoCompositeCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<AutoCompositeCommand>)this).LockGameChar(command);
            if (dw.IsEmpty)
            {
                command.FillErrorFromWorld();
                return; //若锁定失败
            }
            var genus = new HashSet<string> { "p_wh", "p_gn", "p_bu" };
            while (true)
            {
                var ary = command.GameChar.GetAllChildren().Select(c => _EntityManager.GetEntity(c)).OfType<GameEquipment>()
                    .Where(c => //仅选取白绿蓝三色
                    {
                        var g = _TemplateManager.GetFullViewFromId(c.TemplateId)?.Genus;
                        if (!(g?.Length > 0)) return false;
                        return genus.Overlaps(g);
                    })
                    .GroupBy(c => c.TemplateId).Where(c => c.Count() >= 3).ToArray();
                if (ary.Length > 0) //若存在需要合成的物品
                {
                    var allBlueprint = _TemplateManager.Id2FullView.Values
                        .Where(c => c.Out?.Count > 0 && c?.In.Count == 1 && c.In[0].Conditional.Count == 1 && c.In[0].Conditional[0].TId.HasValue)
                        .ToLookup(c => c.In[0].Conditional[0].TId.Value);    //获得所有蓝图
                    bool changed = false;
                    foreach (var group in ary)
                    {
                        var smallAry = group.OrderByDescending(c => c.Level).Chunk(3).ToList();
                        foreach (var items in smallAry)
                        {
                            if (items.Length != 3) continue;  //若不是3个
                            var bp = allBlueprint[items.First().TemplateId].FirstOrDefault();
                            if (bp is null) continue;    //若找不到适用蓝图
                            var subCommand = new CompositeCommand
                            {
                                GameChar = command.GameChar,
                                MainItem = items.First()
                            };
                            subCommand.Items.AddRange(items.Skip(1));
                            subCommand.Blueprint = bp;
                            _SyncCommandManager.Handle(subCommand);
                            if (subCommand.HasError)
                            {
                                command.FillErrorFrom(subCommand);
                                return;
                            }
                            changed = true;
                            command.Changes.AddRange(subCommand.Changes);   //TODO 暂时无法做到原子性
                        }
                    }
                    if (!changed)
                        break;
                }
                else
                    break;
            }

        }
    }
}
