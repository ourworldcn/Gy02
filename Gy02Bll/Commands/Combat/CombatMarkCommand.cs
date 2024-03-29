﻿using GY02.Managers;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class CombatMarkCommand : SyncCommandBase
    {
        public CombatMarkCommand() { }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要标记的战斗信息。
        /// </summary>
        public string CombatInfo { get; set; }
    }

    public class CombatMarkHandler : SyncCommandHandlerBase<CombatMarkCommand>
    {
        public CombatMarkHandler(GameAccountStoreManager gameAccountStore)
        {
            _GameAccountStore = gameAccountStore;
        }

        GameAccountStoreManager _GameAccountStore;

        public override void Handle(CombatMarkCommand command)
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
            command.GameChar.ClientCombatInfo = command.CombatInfo;
            _GameAccountStore.Save(key);
        }
    }
}
