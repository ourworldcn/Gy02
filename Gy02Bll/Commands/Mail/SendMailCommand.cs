using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
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
    public class SendMailItem
    {
        public SendMailItem()
        {

        }

        #region 基本属性

        /// <summary>
        /// 邮件标题。
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 邮件正文。
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 附件集合。
        /// </summary>
        public List<GameEntitySummary> Attachment { get; set; } = new List<GameEntitySummary> { };

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary1 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary2 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 对没有附件且已读的邮件，多长时间删除。若为空则等待最长删除时间到来，当前是60天。
        /// </summary>
        public TimeSpan? DeleteDelay { get; set; }

        #endregion 基本属性

    }

    public class SendMailCommand : SyncCommandBase, IGameCharCommand
    {
        public SendMailCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 邮件的本体。
        /// </summary>
        public SendMailItem Mail { get; set; }

        /// <summary>
        /// 发送的地址，对方角色唯一id的字符串，通常省略（空集合），表示群发，群发不会给自己发送邮件，若需要必须明确指定自己的Id。
        /// </summary>
        public List<Guid> ToIds { get; set; } = new List<Guid>();

    }

    public class SendMailHandler : SyncCommandHandlerBase<SendMailCommand>, IGameCharHandler<SendMailCommand>
    {
        public SendMailHandler(GameAccountStoreManager accountStore, GameMailManager mailManager, SyncCommandManager syncCommandManager)
        {
            AccountStore = accountStore;
            _MailManager = mailManager;
            _SyncCommandManager = syncCommandManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        public GameMailManager _MailManager;

        public SyncCommandManager _SyncCommandManager;

        public override void Handle(SendMailCommand command)
        {
            var key = ((IGameCharHandler<SendMailCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<SendMailCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            string tos;
            if (command.ToIds.Count > 0)
            {
                tos = string.Join(';', command.ToIds.Select(c => c.ToString()));
            }
            else
                tos = null;

            var mail = _MailManager.CreateNewMail();
            mail.Subject = command.Mail.Subject;
            mail.Body = command.Mail.Body;
            OwHelper.Copy(command.Mail.Dictionary1, mail.Dictionary1);
            OwHelper.Copy(command.Mail.Dictionary2, mail.Dictionary2);
            mail.Attachment.AddRange(command.Mail.Attachment.Select(c => (GameEntitySummary)c.Clone()));
            mail.DeleteDelay = command.Mail.DeleteDelay;

            var mails = new List<GameMail>();

            if (!_MailManager.SendMail(command.GameChar, mail, command.GameChar.Key, tos, mails))
                command.FillErrorFromWorld();
            //引发发送邮件后的事件。
            var subCommand = new MailsSendedCommand { GameChar = command.GameChar };
            subCommand.Mails.AddRange(mails);
            try
            {
                _SyncCommandManager.Handle(subCommand);
            }
            catch (Exception)
            {
            }
            AccountStore.Save(key);
            return;

        }
    }
}
