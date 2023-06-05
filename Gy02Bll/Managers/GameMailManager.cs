using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameMailManagerOptions : IOptions<GameMailManagerOptions>
    {
        public GameMailManagerOptions()
        {

        }

        public GameMailManagerOptions Value => this;
    }

    /// <summary>
    /// 管理邮件的服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameMailManager : GameManagerBase<GameMailManagerOptions, GameMailManager>
    {
        public GameMailManager(IOptions<GameMailManagerOptions> options, ILogger<GameMailManager> logger, IDbContextFactory<GY02UserContext> contextFactory, GameEntityManager gameEntityManager, VirtualThingManager virtualThingManager) : base(options, logger)
        {
            _ContextFactory = contextFactory;
            _EntityManager = gameEntityManager;
            _VirtualThingManager = virtualThingManager;
        }

        IDbContextFactory<GY02UserContext> _ContextFactory;
        GameEntityManager _EntityManager;
        VirtualThingManager _VirtualThingManager;

        /// <summary>
        /// 创建一个新的mail对象。
        /// </summary>
        /// <returns></returns>
        public GameMail CreateNewMail()
        {

            var thing = new VirtualThing();
            var result = thing.GetJsonObject<GameMail>();
            result.TemplateId = ProjectContent.MailTId;
            return result;
        }

        public List<GameMail> CreateNewMails(GameMail mail, IEnumerable<string> tos)
        {
            int count = tos.Count();
            var things = _VirtualThingManager.Create(ProjectContent.MailTId, count);
            if (things is null) return null;    //若出错
            var result = new List<GameMail>(count);
            int index = 0;
            foreach (var to in tos)
            {
                var tmp = things[index++].GetJsonObject<GameMail>();
                tmp.From = mail.From;
                tmp.To = to;
                tmp.Subject = mail.Subject;
                tmp.Body = mail.Body;
                tmp.Attachment.AddRange(mail.Attachment.Select(c1 => (GameEntitySummary)c1.Clone()));
                result.Add(tmp);
            }
            return result;
        }

        /// <summary>
        /// 发送邮件。
        /// </summary>
        /// <param name="gameChar">发送邮件的角色。</param>
        /// <param name="mail"></param>
        /// <param name="from">角色Id。</param>
        /// <param name="to">要发送的角色对象，省略或为null则发送给所有人，此时无法给自己发送邮件。也可以用分号分隔发送给多人。</param>
        /// <param name="newMails">若不是空，则追加最新发送的邮件到此集合。注意此时邮件的虚拟实体已经处于分离状态。</param>
        /// <returns></returns>
        public bool SendMail(GameChar gameChar, GameMail mail, string from, string to = null, List<GameMail> newMails = null)
        {
            mail.From = from;
            if (mail.From is null) return false;
            IEnumerable<string> tos;
            using var db = _ContextFactory.CreateDbContext();
            if (to is not null)
            {
                tos = to.Split(OwHelper.SemicolonArrayWithCN);
            }
            else //若发送到所有人
            {
                var coll = db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.CharTId && c.Id != gameChar.Id).Select(c => c.Id).AsEnumerable();
                tos = coll.Select(c => c.ToString());
            }
            if (tos.Any(c => c.Length > 64))
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"发送邮件时出错,至少有一个发送地址大于64个字符。");
                return false;
            }

            var mails = CreateNewMails(mail, tos);
            if (newMails is not null) newMails.AddRange(mails);

            db.AddRange(mails.Select(c => c.GetThing()));
            try
            {
                db.SaveChanges();
            }
            catch (Exception excp)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                OwHelper.SetLastErrorMessage($"发送邮件时出错{excp.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取指定角色的所有收件箱中的邮件。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public List<GameMail> GetMails(GameChar gameChar)
        {
            var to = gameChar.Key;
            using var db = _ContextFactory.CreateDbContext();
            var things = db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.MailTId && c.ExtraString == to);
            var result = things.AsEnumerable().Select(c => c.GetJsonObject<GameMail>()).ToList();
            return result;
        }

        /// <summary>
        /// 收取附件。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="mail"></param>
        /// <returns></returns>
        public bool PickUp(GameChar gameChar, GameMail mail, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (!_EntityManager.CreateAndMove(mail.Attachment.Select(c => (c.TId, c.Count, c.ParentTId)), gameChar, changes))
                return false;
            mail.PickUpUtc = DateTime.UtcNow;
            return true;
        }
    }
}
