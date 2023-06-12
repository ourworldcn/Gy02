using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class GetMailsCommand : SyncCommandBase, IGameCharCommand
    {
        public GetMailsCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 返回收件箱中的邮件。空集合标识没有邮件。
        /// </summary>
        public List<GameMail> Mails { get; set; } = new List<GameMail>();
    }

    public class GetMailsHandler : SyncCommandHandlerBase<GetMailsCommand>, IGameCharHandler<GetMailsCommand>
    {
        public GetMailsHandler(GameAccountStore accountStore, GameMailManager mailManager)
        {
            AccountStore = accountStore;
            _MailManager = mailManager;
        }
        public GameAccountStore AccountStore { get; }

        public GameMailManager _MailManager;

        public override void Handle(GetMailsCommand command)
        {
            var key = ((IGameCharHandler<GetMailsCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<GetMailsCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var mails = _MailManager.GetMails(command.GameChar);
            command.Mails.AddRange(mails);

            AccountStore.Save(key);
            return;

        }
    }
}
