using AutoMapper;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Commands
{
    public class FuhuaPreviewCommand : SyncCommandBase, IGameCharCommand, IValidatableObject
    {
        public FuhuaPreviewCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 双亲的类属集合 如 "zuoqi_sheep" "zuoqi_wolf"，无所谓顺序，但返回时是按升序排序。
        /// </summary>
        public List<string> ParentGenus { get; set; } = new List<string>();

        /// <summary>
        /// 返回数据，孵化可能生成的预览信息列表。
        /// </summary>
        public List<GameDiceItem> Result { get; set; } = new List<GameDiceItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
    }

    public class FuhuaPreviewHandler : SyncCommandHandlerBase<FuhuaPreviewCommand>, IGameCharHandler<FuhuaPreviewCommand>
    {
        public FuhuaPreviewHandler(GameAccountStore gameAccountStore, GameEntityManager gameEntityManager, TemplateManager templateManager, GameDiceManager diceManager, SpecialManager specialManager, IMapper mapper)
        {
            _GameAccountStore = gameAccountStore;
            _GameEntityManager = gameEntityManager;
            _TemplateManager = templateManager;
            _DiceManager = diceManager;
            _SpecialManager = specialManager;
            _Mapper = mapper;
        }

        GameAccountStore _GameAccountStore;
        GameEntityManager _GameEntityManager;
        TemplateManager _TemplateManager;
        GameDiceManager _DiceManager;
        IMapper _Mapper;

        public GameAccountStore AccountStore => _GameAccountStore;

        SpecialManager _SpecialManager;

        public override void Handle(FuhuaPreviewCommand command)
        {
            var key = ((IGameCharHandler<FuhuaPreviewCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<FuhuaPreviewCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var gc = command.GameChar;

            if (command.ParentGenus.Distinct().Count() != command.ParentGenus.Count)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"指定的双亲类属的集合中有重复元素。";
                return;
            }
            command.ParentGenus.Sort();
            var history = _SpecialManager.GetOrAddFuhuaHistory(command.ParentGenus, command.GameChar);   //获取指定双亲的皮肤生成记录

            var preview = _SpecialManager.GetFuhuaSummary(command.ParentGenus, command.GameChar);
            if (preview is null)    //若没有生成的记录
            {
                var info = _SpecialManager.GetFuhuaInfo(command.ParentGenus);
                if (info.Item1 is null) goto lbErr; //若出错
                //var mounts = _SpecialManager.GetOutputs(info.Item2.Dice);

                //var pifus = _SpecialManager.GetOutputs(info.Item3.Dice, history.Items.Select(c => c.Entity.TId));

               var coll= _SpecialManager.GetFuhuaEntitySummary(command.ParentGenus, command.GameChar);
                //var items = _DiceManager.Transformed(coll.Select(c => c.Outs));
                preview = new FuhuaSummary { };
                preview.ParentTIds.AddRange(command.ParentGenus);
                //preview.Items.AddRange(mounts.Select(c => GameDiceManager.GetDiceItemSummary(c)));
                //preview.Items.AddRange(pifus.Select(c => GameDiceManager.GetDiceItemSummary(c)));
                preview.Items.AddRange(coll);
                gc.FuhuaPreview.Add(preview);
            }
            command.Result.AddRange(preview.Items);
            _GameAccountStore.Save(key);    //保存数据
            return;
        lbErr:
            command.FillErrorFromWorld();
        }

    }
}
