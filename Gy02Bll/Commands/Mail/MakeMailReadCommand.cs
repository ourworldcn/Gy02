using GY02.Managers;
using GY02.Publisher;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands.Mail
{
    public class MakeMailReadCommand : SyncCommandBase, IGameCharCommand
    {
        public MakeMailReadCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要标记已读且获取附件的邮件的唯一Id集合。
        /// </summary>
        public List<Guid> MailIds { get; set; } = new List<Guid>();
    }

    public class MakeMailReadHandler : SyncCommandHandlerBase<MakeMailReadCommand>, IGameCharHandler<MakeMailReadCommand>
    {
        public MakeMailReadHandler(GameAccountStore accountStore, GameMailManager mailManager, GY02UserContext dbContext)
        {
            AccountStore = accountStore;
            _MailManager = mailManager;
            DbContext = dbContext;
        }

        public GameAccountStore AccountStore { get; }

        public GameMailManager _MailManager { get; set; }

        public GY02UserContext DbContext { get; set; }

        public override void Handle(MakeMailReadCommand command)
        {
            var key = ((IGameCharHandler<MakeMailReadCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<MakeMailReadCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var mails = _MailManager.GetMails(command.GameChar, DbContext);
            if (mails is null)
            {
                command.FillErrorFromWorld();
                return;
            }
            mails.ForEach(c => c.ReadUtc = DateTime.UtcNow);
            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception err)
            {
                command.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                command.DebugMessage = $"保存数据时出错——{err.Message}";
                return;
            }
            AccountStore.Nop(key);
            return;
        }
    }
}
