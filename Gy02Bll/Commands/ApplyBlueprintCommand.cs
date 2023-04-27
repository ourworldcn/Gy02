﻿using Gy02.Publisher;
using Gy02Bll.Commands.Combat;
using Gy02Bll.Managers;
using Gy02Bll.Templates;
using OW.DDD;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    /// <summary>
    /// 应用蓝图的命令
    /// </summary>
    public class ApplyBlueprintCommand : PropertyChangeCommandBase, IGameCharCommand
    {
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
        public ApplyBlueprintHandler(GameAccountStore gameAccountStore, BlueprintManager blueprintManager, SyncCommandManager syncCommandManager, TemplateManager templateManager, GameEntityManager gameEntityManager)
        {
            _GameAccountStore = gameAccountStore;
            _BlueprintManager = blueprintManager;
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
            _GameEntityManager = gameEntityManager;
        }

        GameAccountStore _GameAccountStore;
        BlueprintManager _BlueprintManager;
        SyncCommandManager _SyncCommandManager;
        TemplateManager _TemplateManager;
        GameEntityManager _GameEntityManager;

        public GameAccountStore AccountStore => _GameAccountStore;

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
            var createThing = new CreateVirtualThingsCommand { };
            createThing.TIds.AddRange(bp.Out.Select(c => c.TId));
            _SyncCommandManager.Handle(createThing);
            var results = _GameEntityManager.Create(createThing.TIds.Select(c => (c, 1m)));
            if (createThing.HasError)
            {
                command.FillErrorFrom(createThing);
                return;
            }
            outs.AddRange(results);
            //消耗材料
            //foreach (var item in command.InItems)
            //{
            //    _GameEntityManager.Modify(item, -item.Count, command.Changes);
            //}
            //_GameEntityManager.Move(outs, command.GameChar, command.Changes);
            command.OutItems.AddRange(results);
            _GameAccountStore.Save(key);
        }
    }
}
