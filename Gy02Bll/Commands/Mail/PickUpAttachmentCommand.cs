using GY02.Managers;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class PickUpAttachmentCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public PickUpAttachmentCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要获取附件的邮件的唯一Id集合。如果是空集合则获取所有邮件的附件。一个邮件的多个附件必须一次性全部获取。
        /// </summary>
        public List<Guid> MailIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 
    /// </summary>
    public class PickUpAttachmentHandler : SyncCommandHandlerBase<PickUpAttachmentCommand>, IGameCharHandler<PickUpAttachmentCommand>
    {
        public PickUpAttachmentHandler(GameAccountStore accountStore, GameEntityManager entityManager, GameMailManager mailManager)
        {
            AccountStore = accountStore;
            _EntityManager = entityManager;
            _MailManager = mailManager;
        }

        public GameAccountStore AccountStore { get; }

        GameEntityManager _EntityManager;
        GameMailManager _MailManager;

        public override void Handle(PickUpAttachmentCommand command)
        {
            var key = ((IGameCharHandler<PickUpAttachmentCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<PickUpAttachmentCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            if (!_MailManager.PickUpAttachment(command.GameChar, command.MailIds, command.Changes))
                goto lbErr;

            AccountStore.Save(key);
            return;
        lbErr:
            command.FillErrorFromWorld();
        }
    }
}
