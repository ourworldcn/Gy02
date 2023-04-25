using Gy02.Publisher;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Item
{
    /// <summary>
    /// 自动合成紫色（不含）以下装备。
    /// </summary>
    public class AutoCompositeCommand : PropertyChangeCommandBase
    {
        public GameChar GameChar { get; set; }
    }

    public class AutoCompositeHandler : SyncCommandHandlerBase<AutoCompositeCommand>
    {
        public AutoCompositeHandler(SyncCommandManager syncCommandManager, TemplateManager templateManager)
        {
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
        }

        SyncCommandManager _SyncCommandManager;
        TemplateManager _TemplateManager;

        public override void Handle(AutoCompositeCommand command)
        {
            while (true)
            {
                var ary = command.GameChar.ZhuangBeiBag.Children.GroupBy(c => c.TemplateId).Where(c => c.Count() >= 3).ToArray();
                if (ary.Length > 0) //若存在需要合成的物品
                {
                    var allBlueprint = _TemplateManager.Id2FullView.Values
                        .Where(c => c.Out?.Count > 0 && c?.In.Count == 1 && c.In[0].Conditional.Count == 1 && c.In[0].Conditional[0].TId.HasValue)
                        .ToLookup(c => c.In[0].Conditional[0].TId.Value);    //获得所有蓝图

                    foreach (var group in ary)
                    {
                        var smallAry = group.Chunk(3);
                        foreach (var items in smallAry)
                        {
                            if (items.Length != 3) continue;  //若不是3个
                            var bp = allBlueprint[items.First().TemplateId].FirstOrDefault();
                            if (bp is null) continue;    //若找不到适用蓝图
                            var subCommand = new CompositeCommand
                            {
                                GameChar = command.GameChar,
                            };
                            subCommand.MainItem = items.First();
                            subCommand.Items.AddRange(items.Skip(1));
                            subCommand.Blueprint = bp;
                            _SyncCommandManager.Handle(subCommand);
                            if (subCommand.HasError)
                            {
                                command.FillErrorFrom(subCommand);
                                return;
                            }
                            command.Changes.AddRange(subCommand.Changes);   //TODO 暂时无法做到原子性
                        }
                    }
                }
                else
                    break;
            }

        }
    }
}
