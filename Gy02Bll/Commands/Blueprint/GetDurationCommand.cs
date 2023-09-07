using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    /// <summary>
    /// 获取当前有效期或最近的下一个有效期的起止时间。
    /// </summary>
    public class GetDurationCommand : SyncCommandBase, IGameCharCommand
    {

        public GetDurationCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 模板Id，通常是关卡TId。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 返回的起始时间。若无有效周期则为null。
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// 返回的终止时间。若无有效周期则为null。
        /// </summary>
        public DateTime? End { get; set; }
    }

    /// <summary>
    /// 处理获取有效期。
    /// </summary>
    public class GetDurationHandler : SyncCommandHandlerBase<GetDurationCommand>, IGameCharHandler<GetDurationCommand>
    {
        public GetDurationHandler(GameAccountStoreManager accountStore, GameTemplateManager templateManager, GameEntityManager entityManager)
        {
            AccountStore = accountStore;
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;

        public override void Handle(GetDurationCommand command)
        {
            if (_TemplateManager.GetFullViewFromId(command.TId) is not TemplateStringFullView tt) goto lbErr;
            if (tt.Ins is not List<BlueprintInItem> list) goto lbErr;
            if (list.FirstOrDefault(c => c.Conditional.Any(d => d.NumberCondition is not null)) is not BlueprintInItem item) goto lbErr;
            var tc = item.Conditional.FirstOrDefault(c => c.NumberCondition is not null);
            var nc = tc.NumberCondition;
            var now = OwHelper.WorldNow;

            var entity = _EntityManager.GetAllEntity(command.GameChar).FirstOrDefault(c =>
            {
                return c.TemplateId == tc.TId;
            });
            if (entity is null) return; //若前置条件实体还不存在
            var count = entity.Count;
            int tmp = 0;
            decimal start, end;
            while (true)
            {
                if (nc.GetCurrentPeriod(count, out start, out end))
                {
                    command.Start = now.Date.AddDays((double)start);
                    command.End=now.Date.AddDays((double)end+1);
                    break;
                }
                else if (++tmp > nc.Modulus) break;
                count++;
            }
            return;
        lbErr:
            command.HasError = true;
            command.FillErrorFromWorld();
        }
    }
}
