﻿using GY02.Managers;
using GY02.Publisher;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class StartCombatCommand : PropertyChangeCommandBase
    {
        public StartCombatCommand()
        {

        }

        public GameChar GameChar { get; set; }

        public Guid CombatTId { get; set; }
    }

    public class StartCombatHandler : SyncCommandHandlerBase<StartCombatCommand>
    {
        public StartCombatHandler(GameAccountStoreManager gameAccountStore, GameTemplateManager templateManager, GameBlueprintManager blueprintManager, GameEntityManager gameEntityManager)
        {
            _GameAccountStore = gameAccountStore;
            _TemplateManager = templateManager;
            _BlueprintManager = blueprintManager;
            _GameEntityManager = gameEntityManager;
        }

        GameAccountStoreManager _GameAccountStore;
        GameTemplateManager _TemplateManager;
        GameBlueprintManager _BlueprintManager;
        GameEntityManager _GameEntityManager;

        public override void Handle(StartCombatCommand command)
        {
            var key = command.GameChar.GetUser()?.Key;
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
            if (command.GameChar.CombatTId is not null && command.CombatTId != command.GameChar.CombatTId)   //若模板Id不符合
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"客户端指定战斗模板Id={command.CombatTId},但用户实际的战斗模板Id={command.GameChar.CombatTId}";
                return;
            }

            var tt = _TemplateManager.GetFullViewFromId(command.CombatTId);
            if (tt is null) return; //若找不到指定模板
            if (command.CombatTId != command.GameChar.CombatTId) //若是首次进入
            {
                List<GameEntity> all = new List<GameEntity>();
                foreach (var thing in command.GameChar.GetAllChildren())
                {
                    var entity = _GameEntityManager.GetEntity(thing);
                    if (entity is null)
                    {
                        command.FillErrorFromWorld();
                        return;
                    }
                    all.Add(entity);
                }
                if (tt.Ins?.Count > 0) //若需要消耗资源
                    if (!_BlueprintManager.Deplete(all, tt.Ins, command.Changes)) { command.FillErrorFromWorld(); return; }
                command.GameChar.CombatTId = command.CombatTId;
                command.Changes?.MarkNewValueChanges(command.GameChar, nameof(command.GameChar.CombatTId), command.GameChar.CombatTId);
            }
            _GameAccountStore.Save(key);
        }
    }
}
