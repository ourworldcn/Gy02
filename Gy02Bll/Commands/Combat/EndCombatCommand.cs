﻿using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Commands
{
    /// <summary>
    /// 结算关卡命令。
    /// </summary>
    public class EndCombatCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public EndCombatCommand()
        {

        }

        /// <summary>
        /// 战斗的角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 战斗关卡的模板Id。
        /// </summary>
        public Guid CombatTId { get; set; }

        /// <summary>
        /// 掉落物品的集合。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<GameEntitySummary> Rewards { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 杀怪或其它集合。
        /// </summary>
        public List<GameEntitySummary> Others { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 看广告后的额外奖励。
        /// </summary>
        public List<GameEntitySummary> AdsRewards { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 该关卡的最短时间，如果null,表示不记录。
        /// </summary>
        public TimeSpan? MinTimeSpanOfPass { get; set; }

        /// <summary>
        /// 是否成功的完成此关卡
        /// </summary>
        /// <value>true成功完成了此管卡，false没有完成。</value>
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class EndCombatHandler : SyncCommandHandlerBase<EndCombatCommand>, IGameCharHandler<EndCombatCommand>
    {
        public EndCombatHandler(GameAccountStoreManager gameAccountStore, GameEntityManager gameEntityManager, SyncCommandManager syncCommandManager)
        {
            _AccountStore = gameAccountStore;
            _GameEntityManager = gameEntityManager;
            _SyncCommandManager = syncCommandManager;
        }

        GameAccountStoreManager _AccountStore;
        GameEntityManager _GameEntityManager;
        SyncCommandManager _SyncCommandManager;

        public GameAccountStoreManager AccountStore => _AccountStore;

        public override void Handle(EndCombatCommand command)
        {
            var key = ((IGameCharHandler<EndCombatCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<EndCombatCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            if (command.CombatTId != command.GameChar.CombatTId)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"客户端指定战斗模板Id={command.CombatTId},但用户实际的战斗模板Id={command.GameChar.CombatTId}";
                return;
            }
            command.GameChar.ClientCombatInfo = null;
            //把掉落物品增加到角色背包中
            var coll = from tmp in command.Rewards
                       group tmp by tmp.TId into g
                       where g.Count() > 0
                       select new GameEntitySummary { TId = g.Key, Count = g.Sum(c => c.Count) };
            var list = _GameEntityManager.Create(coll);
            if (coll.Any()) //若有需要移动的实体
                _GameEntityManager.Move(list.Select(c => c.Item2), command.GameChar, command.Changes);
            var change = command.Changes?.MarkChanges(command.GameChar, nameof(command.GameChar.CombatTId), command.GameChar.CombatTId, null);
            if (change is not null) change.HasNewValue = false;
            command.GameChar.CombatTId = null;

            #region 记录战斗信息
            var gc = command.GameChar;
            var ch = gc.CombatHistory.FirstOrDefault(c => c.TId == command.CombatTId);
            if (ch is null) //若尚未初始化
            {
                ch = new CombatHistoryItem { TId = command.CombatTId };
                gc.CombatHistory.Add(ch);
            }
            if (!ch.MinTimeSpanOfPass.HasValue || ch.MinTimeSpanOfPass > command.MinTimeSpanOfPass)
            {
                var change1 = command.Changes?.MarkChanges(gc, nameof(gc.CombatHistory), new CombatHistoryItem { TId = ch.TId, MinTimeSpanOfPass = ch.MinTimeSpanOfPass }, ch);
                if (change1 != null)
                {
                    change1.HasOldValue = ch.MinTimeSpanOfPass.HasValue;
                    change1.HasNewValue = true;
                }
                ch.MinTimeSpanOfPass = command.MinTimeSpanOfPass;
            }

            #endregion 记录战斗信息

            #region 记录看广告后额外奖励信息
            gc.AdsRewardsHistory.Clear();
            gc.AdsRewardsHistory.AddRange(command.AdsRewards.Select(c => c.Clone() as GameEntitySummary));
            #endregion  记录看广告后额外奖励信息

            #region 金猪活动
            /*311bae23-09b4-4d8b-b158-f3129d5f6503	金猪累计金币占位符
            a84bcbd1-9541-4907-99df-59b19559ae9f	金猪累计钻石占位符*/
            var jinzhuColl = command.AdsRewards.Concat(command.Rewards);
            var jinzhuList = new List<GameEntitySummary>();
            var jinzhuJinbi = jinzhuColl.Where(c => c.TId == ProjectContent.GoldTId).Sum(c => c.Count); //如果source不包含任何元素，则方法返回零。
            if (jinzhuJinbi > 0)
                jinzhuList.Add(new GameEntitySummary { TId = Guid.Parse("311bae23-09b4-4d8b-b158-f3129d5f6503"), Count = jinzhuJinbi });
            var jinzhuZuanshi = jinzhuColl.Where(c => c.TId == ProjectContent.DiamTId).Sum(c => c.Count); //如果source不包含任何元素，则方法返回零。
            if (jinzhuZuanshi > 0)
                jinzhuList.Add(new GameEntitySummary { TId = Guid.Parse("a84bcbd1-9541-4907-99df-59b19559ae9f"), Count = jinzhuJinbi });
            if (jinzhuList.Count > 0)
                _GameEntityManager.CreateAndMove(jinzhuList, gc, command.Changes);
            #endregion 金猪活动
            _AccountStore.Save(key);
        }

    }

}
