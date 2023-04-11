﻿using Gy02.Publisher;
using Gy02Bll.Managers;
using Gy02Bll.Templates;
using OW.DDD;
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
    public class ApplyBlueprintCommand : PropertyChangeCommandBase
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

    public class ApplyBlueprintHandler : SyncCommandHandlerBase<ApplyBlueprintCommand>
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

        public override void Handle(ApplyBlueprintCommand command)
        {
            string key = command.GameChar.GetUser().GetKey();
            if (!_GameAccountStore.Lock(key))   //若锁定失败
            {
                command.FillErrorFromWorld();
                return;
            }
            using var dw = DisposeHelper.Create(_GameAccountStore.Unlock, key);
            if (dw.IsEmpty) //若锁定失败
            {
                command.FillErrorFromWorld();
                return;
            }
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
            if (createThing.HasError)
            {
                command.FillErrorFrom(createThing);
                return;
            }
            outs.AddRange(createThing.Result.Select(c => _GameEntityManager.GetEntity(c)));
            //消耗材料
            //foreach (var item in command.InItems)
            //{
            //    _GameEntityManager.Modify(item, -item.Count, command.Changes);
            //}
            //_GameEntityManager.Move(outs, command.GameChar, command.Changes);
            command.OutItems.AddRange(outs);
        }
    }
}
