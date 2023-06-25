using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
                OwHelper.Copy(mail.Dictionary1, tmp.Dictionary1);
                OwHelper.Copy(mail.Dictionary2, tmp.Dictionary2);
                tmp.SendUtc = DateTime.UtcNow;
                tmp.DeleteDelay = mail.DeleteDelay;
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
                var coll = db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.CharTId /*&& c.Id != gameChar.Id*/).Select(c => c.Id).AsEnumerable();
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
        /// <param name="dbContext">使用的数据库上下文，若不指定则自动生成（此时也会自动处置），若指定了则不会处置。</param>
        /// <returns></returns>
        public List<GameMail> GetMails(GameChar gameChar, DbContext dbContext = null)
        {
            var to = gameChar.Key;
            bool bDbOwner = dbContext is null;  //函数自身拥有上下文
            if (bDbOwner) dbContext = _ContextFactory.CreateDbContext();
            var result = new List<GameMail> { };
            try
            {
                var things = dbContext.Set<VirtualThing>().Where(c => c.ExtraGuid == ProjectContent.MailTId && c.ExtraString == to).AsEnumerable();
                result = things.Select(c => c.GetJsonObject<GameMail>()).ToList();
            }
            finally
            {
                if (bDbOwner) dbContext.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 获取指定邮件的附件。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="mailIds">要获取附件的邮件的唯一Id集合。如果是空集合则获取所有邮件的附件。一个邮件的多个附件必须一次性全部获取。</param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public bool PickUpAttachment(GameChar gameChar, List<Guid> mailIds, List<GamePropertyChangeItem<object>> changes)
        {
            using var db = _ContextFactory.CreateDbContext();
            var mails = GetMails(gameChar, db);
            IEnumerable<GameMail> doMails;
            if (mails.Count > 0)  //若选择邮件
                doMails = mails.Where(mail => mailIds.Contains(mail.Id));
            else doMails = mails;
            doMails = doMails.Where(c => c.PickUpUtc is null);
            //var errFirst = doMails.FirstOrDefault(c => c.PickUpUtc is not null);
            //if (errFirst is not null)    //若至少一个邮件的附件已经被领取
            //{
            //    OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
            //    OwHelper.SetLastErrorMessage($"至少一个邮件的附件已经被领取，MailId={errFirst.Id}");
            //    return false;
            //}
            var summary = doMails.SelectMany(c => c.Attachment);
            var nowUtc = DateTime.UtcNow;
            if (!_EntityManager.CreateAndMove(summary, gameChar, changes))
                return false;
            doMails.ForEach(c => c.PickUpUtc = nowUtc);
            db.SaveChanges();
            return true;
        }

        /// <summary>
        /// 清理过期邮件。
        /// </summary>
        public void ClearMail()
        {
            using var db = _ContextFactory.CreateDbContext();
            var limit = (DateTime.UtcNow - TimeSpan.FromDays(60)).ToString();   //60天之前的时间点
            var coll = from mail in db.Set<VirtualThing>()
                       where mail.ExtraGuid == ProjectContent.MailTId && (SqlDbFunctions.JsonValue(mail.JsonObjectString, "$.ReadUtc") != null || SqlDbFunctions.JsonValue(mail.JsonObjectString, "$.PickUpUtc") != null ||
                        StringComparer.CurrentCulture.Compare(SqlDbFunctions.JsonValue(mail.JsonObjectString, "$.SendUtc"), limit) < 0)
                       select mail;
            var mails = coll.AsEnumerable().Select(c => c.GetJsonObject<GameMail>());
            List<VirtualThing> remove = new List<VirtualThing>();
            foreach (var mail in mails)
            {
                if (!IsExpiration(mail)) continue;
                remove.Add(mail.GetThing());
            }
            try
            {
                db.RemoveRange(remove);
                db.SaveChanges();
            }
            catch (Exception)
            {
            }
        }

        public bool IsExpiration(GameMail mail)
        {
            if (mail.DeleteDelay.HasValue)   //若有指定超期
            {
                DateTime last = DateTime.UtcNow;
                if (mail.Attachment.Count > 0 && mail.PickUpUtc.HasValue)   //若有拾取附件的时间
                    last = mail.PickUpUtc.Value;
                if (mail.ReadUtc.HasValue)  //若有已读时间
                    last = last < mail.ReadUtc.Value ? mail.ReadUtc.Value : last;
                if (DateTime.UtcNow - last > mail.DeleteDelay.Value)  //若到期
                    return true;
            }
            if (DateTime.UtcNow - mail.SendUtc >= TimeSpan.FromDays(60))
                return true;
            return false;
        }
    }
}
