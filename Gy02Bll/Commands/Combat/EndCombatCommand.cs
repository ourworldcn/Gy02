using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
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
        public EndCombatHandler(GameAccountStoreManager gameAccountStore, GameEntityManager gameEntityManager, SyncCommandManager syncCommandManager, GameTemplateManager templateManager, GameEventManager eventManager)
        {
            _AccountStore = gameAccountStore;
            _EntityManager = gameEntityManager;
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
            _EventManager = eventManager;
        }

        GameAccountStoreManager _AccountStore;
        GameEntityManager _EntityManager;
        SyncCommandManager _SyncCommandManager;
        GameTemplateManager _TemplateManager;
        GameEventManager _EventManager;

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
            var now = OwHelper.WorldNow;
            var gc = command.GameChar;
            #region 爬塔相关
            var tt = _TemplateManager.GetFullViewFromId(command.CombatTId);
            if (tt.Genus?.Contains(GameCombatManager.PataGenusString) ?? false)    //若是爬塔
            {
                var commandShopping = new ShoppingBuyCommand
                {
                    GameChar = command.GameChar,
                    Count = 1,
                    Changes = command.Changes,
                };
                var ph = _EntityManager.GetEntity(_EntityManager.GetAllEntity(gc), new GameEntitySummary { TId = Guid.Parse("43ADC188-7B1D-4C73-983F-4E5583CBACCD") });    //爬塔占位符
                var ov = ph.Count;

                if (gc.TowerInfo.HardId == command.CombatTId && !gc.TowerInfo.IsHardDone.HasValue)  //若是上手
                {
                    if (command.IsSuccess)   //若赢了
                    {
                        commandShopping.ShoppingItemTId = tt.PataOutIds[0];
                        _SyncCommandManager.Handle(commandShopping);
                        if (commandShopping.HasError)
                        {
                            command.FillErrorFrom(commandShopping);
                            return;
                        }
                        ph.Count += 3;
                        command.Changes?.MarkChanges(ph, nameof(ph.Count), ov, ph.Count);
                    }
                    gc.TowerInfo.IsHardDone = command.IsSuccess;
                }
                else if (gc.TowerInfo.NormalId == command.CombatTId && !gc.TowerInfo.IsNormalDone.HasValue)  //若是平手
                {
                    if (command.IsSuccess)   //若赢了
                    {
                        commandShopping.ShoppingItemTId = tt.PataOutIds[1];
                        _SyncCommandManager.Handle(commandShopping);
                        if (commandShopping.HasError)
                        {
                            command.FillErrorFrom(commandShopping);
                            return;
                        }
                        ph.Count += 2;
                        command.Changes?.MarkChanges(ph, nameof(ph.Count), ov, ph.Count);
                    }
                    gc.TowerInfo.IsNormalDone = command.IsSuccess;
                }
                else if (gc.TowerInfo.EasyId == command.CombatTId && !gc.TowerInfo.IsEasyDone.HasValue)  //若是下手
                {
                    if (command.IsSuccess)   //若赢了
                    {
                        commandShopping.ShoppingItemTId = tt.PataOutIds[2];
                        _SyncCommandManager.Handle(commandShopping);
                        if (commandShopping.HasError)
                        {
                            command.FillErrorFrom(commandShopping);
                            return;
                        }
                        ph.Count += 1;
                        command.Changes?.MarkChanges(ph, nameof(ph.Count), ov, ph.Count);
                    }
                    gc.TowerInfo.IsEasyDone = command.IsSuccess;
                }
                else //非法
                {
                    command.HasError = true;
                    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    command.DebugMessage = "不可进入该塔层";
                    return;
                }
                List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
                if (command.IsSuccess)  //若胜利
                {
                    changes.Clear();
                    commandShopping = new ShoppingBuyCommand
                    {
                        GameChar = command.GameChar,
                        ShoppingItemTId = Guid.Parse("95f0e2cf-757a-4668-aab3-d91db54d1e19"),
                        Count = 1,
                        Changes = changes,
                    };
                    _SyncCommandManager.Handle(commandShopping);
                    if (commandShopping.HasError)
                    {
                        command.FillErrorFrom(commandShopping);
                        return;
                    }
                    command.Changes.AddRange(changes);
                }
                else //若失败
                {
                    changes.Clear();
                    commandShopping = new ShoppingBuyCommand
                    {
                        GameChar = command.GameChar,
                        ShoppingItemTId = Guid.Parse("61a13698-2d2d-4808-b44f-db1f7ac94a40"),
                        Count = 1,
                        Changes = changes,
                    };
                    _SyncCommandManager.Handle(commandShopping);
                    if (commandShopping.HasError)
                    {
                        command.FillErrorFrom(commandShopping);
                        return;
                    }
                    command.Changes.AddRange(changes);
                }
                //事件
                SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
                //竞技场挑战次数的事件	7f6482c8-511a-477e-ab79-ce0d8b7643ca
                _EventManager.SendEvent(Guid.Parse("7f6482c8-511a-477e-ab79-ce0d8b7643ca"), 1, context);

                //竞技场获胜次数的事件 f619df3b-3475-4b28-b291-48aa9014ae7c
                if (command.IsSuccess) _EventManager.SendEvent(Guid.Parse("f619df3b-3475-4b28-b291-48aa9014ae7c"), 1, context);

            }

            #endregion 爬塔相关
            command.GameChar.ClientCombatInfo = null;
            //把掉落物品增加到角色背包中
            var coll = from tmp in command.Rewards
                       group tmp by tmp.TId into g
                       where g.Count() > 0
                       select new GameEntitySummary { TId = g.Key, Count = g.Sum(c => c.Count) };
            var list = _EntityManager.Create(coll);
            if (coll.Any()) //若有需要移动的实体
                _EntityManager.Move(list.Select(c => c.Item2), command.GameChar, command.Changes);
            var change = command.Changes?.MarkChanges(command.GameChar, nameof(command.GameChar.CombatTId), command.GameChar.CombatTId, null);
            if (change is not null) change.HasNewValue = false;
            command.GameChar.CombatTId = null;

            #region 记录战斗信息
            var ch = gc.CombatHistory.FirstOrDefault(c => c.TId == command.CombatTId);
            if (ch is null) //若尚未初始化
            {
                ch = new CombatHistoryItem { TId = command.CombatTId };
                gc.CombatHistory.Add(ch);
            }
            #region 星级评定
            var ttCombat = _TemplateManager.GetFullViewFromId(command.CombatTId);   //管卡模板
            int? lastLevelOfPass = null;
            int? maxLevelOfPass = ch.MaxLevelOfPass;
            if (command.IsSuccess && ttCombat.ScoreTime.Count > 0 && command.MinTimeSpanOfPass.HasValue)  //若需要评定星级
            {
                var tongguan = _EntityManager.GetAllEntity(gc).FirstOrDefault(c => c.TemplateId == ProjectContent.TongguanBiTId); //通关币
                if (tongguan is null)
                {
                    if (!_EntityManager.CreateAndMove(new GameEntitySummary[]{ new GameEntitySummary { TId = ProjectContent.TongguanBiTId, Count = 0, }
                    }, gc, command.Changes))
                    {
                        command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        command.DebugMessage = $"需要评定通关等级，但客户没有通关币对象且无法创建。";
                        return;
                    }
                    tongguan = _EntityManager.GetAllEntity(gc).FirstOrDefault(c => c.TemplateId == ProjectContent.TongguanBiTId); //通关币
                }
                var scope = (decimal)command.MinTimeSpanOfPass.Value.TotalSeconds;   //通关的秒数
                var combarLv = ttCombat.ScoreTime.FindIndex(c => scope <= c);   //星级
                if (combarLv == -1)
                    combarLv = 0;
                else
                    combarLv = ttCombat.ScoreTime.Count - combarLv; //计算得到星级
                if (ch.MaxLevelOfPass is null || ch.MaxLevelOfPass != combarLv)   //若通关等级发生变化
                {
                    var bi = combarLv - (ch.MaxLevelOfPass ?? 0);
                    if (bi > 0)    //若需要增加通关币
                    {
                        var biEntity = new GameEntitySummary[] { new GameEntitySummary {
                            TId=ProjectContent.TongguanBiTId,
                            Count= bi,
                        } };
                        _EntityManager.CreateAndMove(biEntity, gc, command.Changes);
                        maxLevelOfPass = combarLv;
                    }
                }
                lastLevelOfPass = combarLv;
            }
            else
                lastLevelOfPass = null;
            #endregion 星级评定
            //if (!ch.MinTimeSpanOfPass.HasValue || ch.MinTimeSpanOfPass > command.MinTimeSpanOfPass)
            var change1 = command.Changes?.MarkChanges(gc, nameof(gc.CombatHistory), new CombatHistoryItem
            {
                TId = ch.TId,
                MinTimeSpanOfPass = ch.MinTimeSpanOfPass,
                MaxLevelOfPass = ch.MaxLevelOfPass,
                LastLevelOfPass = ch.LastLevelOfPass,
            }, ch);
            if (change1 != null)
            {
                change1.HasOldValue = ch.MinTimeSpanOfPass.HasValue;
                change1.HasNewValue = true;
            }
            if (!ch.MinTimeSpanOfPass.HasValue || ch.MinTimeSpanOfPass > command.MinTimeSpanOfPass)
                ch.MinTimeSpanOfPass = command.MinTimeSpanOfPass;
            ch.MaxLevelOfPass = maxLevelOfPass;
            ch.LastLevelOfPass = lastLevelOfPass;
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
                _EntityManager.CreateAndMove(jinzhuList, gc, command.Changes);
            #endregion 金猪活动

            _AccountStore.Save(key);
        }

    }

}
