using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
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
        public GameMailManager(IOptions<GameMailManagerOptions> options, ILogger<GameMailManager> logger, IDbContextFactory<GY02UserContext> contextFactory, GameEntityManager gameEntityManager) : base(options, logger)
        {
            _ContextFactory = contextFactory;
            _GameEntityManager = gameEntityManager;
        }

        IDbContextFactory<GY02UserContext> _ContextFactory;
        GameEntityManager _GameEntityManager;

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

        public List<VirtualThing> CreateNewMails(GameMail mail, IEnumerable<string> tos)
        {
            var mails = tos.Select(c =>
            {
                var r = CreateNewMail();
                r.From = mail.From;
                r.To = c;
                r.Subject = mail.Subject;
                r.Body = mail.Body;
                r.Attachment.AddRange(mail.Attachment.Select(c1 => (GameEntitySummary)c1.Clone()));
                return r.GetThing();
            });
            return mails.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="from"></param>
        /// <param name="to">要发送的角色对象，省略或为null则发送给所有人。也可以用分号分隔发送给多人。</param>
        /// <returns></returns>
        public bool SendMail(GameMail mail, string from, string to = null)
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
                var coll = db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.CharTId).Select(c => c.Id).AsEnumerable();
                tos = coll.Select(c => c.ToString());
            }
            if (tos.Any(c => c.Length > 64))
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"发送邮件时出错,至少有一个发送地址大于64个字符。");
                return false;
            }

            var mails = CreateNewMails(mail, tos);
            db.AddRange(mails);
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
            var to = gameChar.GetThing().IdString;
            using var db = _ContextFactory.CreateDbContext();
            var coll = db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.MailTId && c.ExtraString == to);
            var result = coll.ToList().Select(c => c.GetJsonObject<GameMail>()).ToList();
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
            if (!_GameEntityManager.CreateAndMove(mail.Attachment.Select(c => (c.TId, c.Count, c.ParentTId)), gameChar, changes))
                return false;
            mail.PickUpUtc = DateTime.UtcNow;
            return true;
        }
    }
}
