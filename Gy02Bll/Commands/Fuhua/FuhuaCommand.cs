﻿using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.SyncCommand;

namespace GY02.Commands
{
    public class FuhuaCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public FuhuaCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 双亲的类属集合 如 "zuoqi_sheep" "zuoqi_wolf"，无所谓顺序，但返回时是按升序排序。
        /// </summary>
        public List<string> ParentGenus { get; set; } = new List<string>();

    }

    public class FuhuaHandler : SyncCommandHandlerBase<FuhuaCommand>, IGameCharHandler<FuhuaCommand>
    {
        public FuhuaHandler(GameAccountStoreManager accountStore, SyncCommandManager syncCommandManager, GameEntityManager gameEntityManager, GameTemplateManager templateManager, GameBlueprintManager blueprintManager,
            SpecialManager specialManager, GameDiceManager diceManager)
        {
            _AccountStore = accountStore;
            _SyncCommandManager = syncCommandManager;
            _GameEntityManager = gameEntityManager;
            _TemplateManager = templateManager;
            _BlueprintManager = blueprintManager;
            _SpecialManager = specialManager;
            _DiceManager = diceManager;
        }

        GameAccountStoreManager _AccountStore;
        public GameAccountStoreManager AccountStore => _AccountStore;

        SyncCommandManager _SyncCommandManager;

        GameTemplateManager _TemplateManager;

        GameDiceManager _DiceManager;
        GameEntityManager _GameEntityManager;


        GameBlueprintManager _BlueprintManager;
        SpecialManager _SpecialManager;

        public override void Handle(FuhuaCommand command)
        {
            var key = ((IGameCharHandler<FuhuaCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<FuhuaCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var gc = command.GameChar;

            command.ParentGenus.Sort();

            #region 条件验证

            if (!Contains(command.ParentGenus, gc))   //若不符合孵化条件
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = $"孵化所需坐骑不全。";
                return;
            }

            #endregion 条件验证

            var preview = gc.FuhuaPreview.FirstOrDefault(c => c.ParentTIds.SequenceEqual(command.ParentGenus));
            if (preview is null)    //若没有生成的记录
            {
                var subCommand = new FuhuaPreviewCommand { GameChar = gc, };
                subCommand.ParentGenus.AddRange(command.ParentGenus);
                _SyncCommandManager.Handle(subCommand);
                if (subCommand.HasError) { command.FillErrorFrom(subCommand); return; }   //若出错
                preview = gc.FuhuaPreview.FirstOrDefault(c => c.ParentTIds.SequenceEqual(command.ParentGenus));
            }

            #region 计算通用消耗
            var info = _SpecialManager.GetFuhuaInfo(command.ParentGenus);
            if (info.Item1 is null) goto lbErr; //若出错
            var tt = info.Item1;
            if (tt.Fuhua.In.Count > 0)  //若有通用消耗
                if (!_BlueprintManager.Deplete(gc.GetAllChildren().Select(c => _GameEntityManager.GetEntity(c)), tt.Fuhua.In, command.Changes)) goto lbErr;
            #endregion 计算通用消耗

            #region 生成孵化专有产出
            var specOut = OwHelper.GetRandom(preview.Items.Select(c => (c, c.Weight)));
            if (specOut is null) goto lbErr;
            var dices = _DiceManager.GetOutputs(Array.Empty<GameDiceItem>().Append(specOut.Value.Item1)).SelectMany(c => c.Item2);
            var items = _GameEntityManager.Create(dices);
            if (items is null) goto lbErr;
            var specOutPtid = _GameEntityManager.GetTemplate(items.First().Item2).ParentTId;
            _GameEntityManager.Move(items.Select(c => c.Item2), gc, command.Changes);
            if (specOutPtid == ProjectContent.PiFuBagTId)    //若是皮肤
            {
                var history = gc.FuhuaHistory.FirstOrDefault(c => c.ParentTIds.SequenceEqual(command.ParentGenus));
                if (history is null)    //若没有找到指定双亲的皮肤生成记录
                {
                    history = new FuhuaSummary { };
                    history.ParentTIds.AddRange(command.ParentGenus);
                    gc.FuhuaHistory.Add(history);
                }
                var tmp = (GameDiceItem)specOut.Value.Item1.Clone();
                history.Items.Add(tmp);

                command.Changes?.Add(new GamePropertyChangeItem<object>
                {
                    Object = gc,
                    PropertyName = nameof(gc.FuhuaHistory),
                    HasOldValue = false,

                    HasNewValue = true,
                    NewValue = tmp,
                });
            }
            gc.FuhuaPreview.Remove(preview);    //移除预览信息
            #endregion 生成孵化专有产出

            _AccountStore.Save(key);    //保存数据
            return;
        lbErr:
            command.FillErrorFromWorld();
        }

        bool Contains(IEnumerable<string> genus, GameChar gc)
        {
            return genus.All(g =>
              {
                  return gc.GetAllChildren().Any(c =>
                  {
                      var tmp = _TemplateManager.GetFullViewFromId(c.ExtraGuid)?.Genus;
                      if (tmp is null)
                          return false;
                      return tmp.Contains(g);
                  });

              });
        }
    }
}
