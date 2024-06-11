using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    /// <summary>
    /// 指定角色今日首次由用户登录后发生该事件。
    /// </summary>
    public class CharFirstLoginedCommand : SyncCommandBase
    {
        public CharFirstLoginedCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 登录的时间点。
        /// </summary>
        public DateTime LoginDateTimeUtc { get; set; }
    }

    public class ButieOptions : IOptions<ButieOptions>
    {
        //LoginNameGeneratorOptions
        public ButieOptions Value => this;

        /// <summary>
        /// 补贴金额，键是uid,值是金额
        /// </summary>
        public Dictionary<string, decimal> Amount { get; set; } = new Dictionary<string, decimal>();
    }

    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<CharFirstLoginedCommand>))]
    public class CharFirstLoginedHandled : ISyncCommandHandled<CharFirstLoginedCommand>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="templateManager"></param>
        /// <param name="achievementManager"></param>
        /// <param name="eventManager"></param>
        /// <param name="mailManager"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="environment"></param>
        public CharFirstLoginedHandled(GameEntityManager entityManager, GameTemplateManager templateManager, GameAchievementManager achievementManager,
            GameEventManager eventManager, GameMailManager mailManager, SyncCommandManager syncCommandManager, IHostEnvironment environment, IOptions<ButieOptions> butieOptions)
        {
            _EntityManager = entityManager;
            _TemplateManager = templateManager;
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
            _MailManager = mailManager;
            _SyncCommandManager = syncCommandManager;
            _Environment = environment;
            _ButieOptions = butieOptions;
        }

        GameEntityManager _EntityManager;
        GameTemplateManager _TemplateManager;
        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;
        GameMailManager _MailManager;
        SyncCommandManager _SyncCommandManager;
        IHostEnvironment _Environment;
        IOptions<ButieOptions> _ButieOptions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exception"></param>
        public void Handled(CharFirstLoginedCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);

            //fl_ExistsDayNumber 此类实体在每天第一次登录时会自动把Count置为该实体存在的总天数，从0开始。副作用，此类属实体的Count设置由系统完成单独设置无用
            var allEntityAndTemplate = _EntityManager.GetAllEntity(command.GameChar).Select(c => (Entity: c, Template: _TemplateManager.GetFullViewFromId(c.TemplateId)))
                .Where(c => c.Item2 is not null).Where(c => c.Template.Genus?.Contains(ProjectContent.ExistsDayNumberGenus) ?? false); //容错
            allEntityAndTemplate.ForEach(c =>
            {
                if (c.Entity.CreateDateTime is null)
                    c.Entity.CreateDateTime = OwHelper.WorldNow;
                c.Entity.Count = (now.Date - c.Entity.CreateDateTime.Value.Date).Days;
            });
            //fl_AutoInc 此类实体在每天第一次登录时会自动把Count+1，从0开始。
            var allEntity = _EntityManager.GetAllEntity(command.GameChar).Select(c => (Entity: c, Template: _TemplateManager.GetFullViewFromId(c.TemplateId)))
                 .Where(c => c.Item2 is not null).Where(c => c.Template.Genus?.Contains(ProjectContent.AutoIncGenus) ?? false);
            allEntity.SafeForEach(c =>
            {
                if (c.Entity.CreateDateTime.Value.Date < now.Date)
                {
                    c.Entity.Count++;
                }
            });
            //f77691d3-2916-42c2-88d7-339febc791fa	登录游戏天数变化事件
            _EventManager.SendEvent(Guid.Parse("f77691d3-2916-42c2-88d7-339febc791fa"), 1, context);

            //发送欢迎邮件

            if (command.GameChar.LogineCount <= 1)  //若第一次登录
            {
                #region 第一封
                var commandMail = new SendMailCommand
                {
                    GameChar = command.GameChar,
                    Mail = new SendMailItem
                    {
                        Subject = "Public Test Celebration Email",
                        Body = "Hi heroes,\r\nTo celebrate the official open beta of \"Animals BAM BAM\" today, and to thank you for your enthusiasm and support, we hereby offer you [100 diamonds]. \r\nWish all heroes an enjoyable gaming experience!",
                    },
                };
                commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {
                    { "English", "Public Test Celebration Email" },
                    { "Chinese", "公测庆祝邮件"},
                    { "Filipino", "Recharge Rebate Email"},
                    { "Indonesian", "Email Rabat Isi Ulang"},
                    { "Malay", "Mengisi semula e -mel rebat"},
                    { "Thai", "เติมเงินคืนอีเมล"},
                };
                commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {
                    { "English", "Hi heroes,\r\nTo celebrate the official open beta of \"Animals BAM BAM\" today, and to thank you for your enthusiasm and support, we hereby offer you [100 diamonds]. \r\nWish all heroes an enjoyable gaming experience!" },
                    { "Chinese", "嗨 英雄，\r\n为了庆祝《Animals BAMBAM》 今日正式公测，为了感谢大家的热情与支持，我们特此奉上【钻石*100】。\r\n祝各位英雄游戏愉快！"},
                    { "Filipino", "Hi hero,\r\nIto ang iyong rebate ng recharge, mangyaring bigyang -pansin upang suriin."},
                    { "Indonesian", "Hai Pahlawan,\r\nIni adalah rabat isi ulang Anda, harap perhatikan untuk memeriksa."},
                    { "Malay", "Hai wira,\r\nIni adalah rebat cas semula anda, sila perhatikan untuk diperiksa."},
                    { "Thai", "สวัสดีฮีโร่\r\nนี่คือการคืนเงินเติมเงินของคุณโปรดใส่ใจในการตรวจสอบ"},
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("c9575f24-a33d-49ba-b130-29b6ff4d62c7"),
                    Count = 100,
                });     //加入附件
                _SyncCommandManager.Handle(commandMail);
                #endregion 第一封

                #region 第二封
                commandMail = new SendMailCommand
                {
                    GameChar = command.GameChar,
                    Mail = new SendMailItem
                    {
                        Subject = "Welcome to the open beta",
                        Body = "Hi heroes,\r\nTo celebrate the official launch of the paid closed beta test of \"Animals BAM BAM\" today, and to thank you for your enthusiasm and support, we hereby offer you [100 diamonds]. \r\nWish all heroes an enjoyable gaming experience!",
                    },
                };
                commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {
                    { "Chinese", "欢迎参加公开测试" },
                    { "Filipino", "Welcome sa pagsali sa open beta"},
                    { "Indonesian", "Selamat datang di beta publik"},
                    { "Malay", "Selamat datang ke Beta Terbuka"},
                    { "Thai", "ยินดีต้อนรับสู่การทดสอบเบต้า"},
                };
                commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {
                    { "Chinese", "嗨 英雄，\r\n为了庆祝《Animals BAMBAM》 今日正式开启付费删档测试，为了感谢大家的热情与支持，我们特此奉上【钻石*100】。\r\n祝各位英雄游戏愉快！" },
                    { "Filipino", "Kamusta Mga Hero,\r\nPara ipagdiwang ang opisyal na paglulunsad ng paid closed beta test ng \"Animals BAM BAM\" ngayon, at para pasalamatan ka para sa iyong suporta at sigasig, kami ay nag-aalok sa iyo ng [100 diamonds] \r\nHangad namin na lahat ng mga hero ay makakaranas ng mahusay na karanasan sa paglalaro!"},
                    { "Indonesian", "Hai, pahlawan!\r\nUntuk merayakan peluncuran resmi beta berbayar penghapusan data \"Animals BAMBAM\" hari ini, dan untuk berterima kasih kepada semua orang atas antusiasme dan dukungannya, dengan ini kami berikan [Diamond*100]. \r\nSelamat bermain, para pahlawan!"},
                    { "Malay", "Hai, para wira,\r\nUntuk meraikan permulaan rasmi ujian beta berbayar \"Animals BAM BAM\" hari ini serta mengucapkan terima kasih atas semangat dan sokongan para wira, kami akan menghadiahkan [Berlian * 100]. \r\nPara wira, selamat bermain!"},
                    { "Thai", "สวัสดี ฮีโร่\r\nเพื่อฉลองเปิดให้ทดสอบลบไฟล์แบบชำระเงินเกม \"Animals BAMBAM\" อย่างเป็นทางการในวันนี้ และเพื่อขอบคุณสำหรับความกระตือรือร้นและการสนับสนุนจากทุกคน เราจึงขอมอบ [เพชร*100] ให้กับคุณ \r\nขอให้ฮีโร่ทุกท่านสนุกกับการเล่นเกมนะ!"},
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("c9575f24-a33d-49ba-b130-29b6ff4d62c7"),
                    Count = 100,
                });    //加入附件
                       //_SyncCommandManager.Handle(commandMail);
            #endregion 第二封
            //E:\BACKUP\GameLoggingjieyou.bak
            //E:\BACKUP\GY02Templatejieyou.bak
            //E:\BACKUP\GY02Userjieyou.bak
                #region 第三封
                if (_ButieOptions.Value.Amount.TryGetValue(command.GameChar.GetUser().LoginName, out var amount))   //若需要补偿
                {
                    commandMail = new SendMailCommand
                    {
                        GameChar = command.GameChar,
                        Mail = new SendMailItem
                        {
                            Subject = "Recharge rebate email",
                            Body = "Hi hero,\r\nThis is your recharge rebate, please pay attention to check.",
                        },
                    };
                    commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {
                    { "English", "Recharge rebate email" },
                    { "Chinese", "充值返利邮件"},
                    { "Filipino", "Recharge Rebate Email"},
                    { "Indonesian", "Email Rabat Isi Ulang"},
                    { "Malay", "Mengisi semula e -mel rebat"},
                    { "Thai", "เติมเงินคืนอีเมล"},
                };
                    commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {
                    { "English", "Hi hero,\r\nThis is your recharge rebate, please pay attention to check." },
                    { "Chinese", "嗨 英雄，\r\n这是您的充值返利，请注意查收。"},
                    { "Filipino", "Hi hero,\r\nIto ang iyong rebate ng recharge, mangyaring bigyang -pansin upang suriin."},
                    { "Indonesian", "Hai Pahlawan,\r\nIni adalah rabat isi ulang Anda, harap perhatikan untuk memeriksa."},
                    { "Malay", "Hai wira,\r\nIni adalah rebat cas semula anda, sila perhatikan untuk diperiksa."},
                    { "Thai", "สวัสดีฮีโร่\r\nนี่คือการคืนเงินเติมเงินของคุณโปรดใส่ใจในการตรวจสอบ"},
                };
                    commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                    commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                    {
                        TId = Guid.Parse("c9575f24-a33d-49ba-b130-29b6ff4d62c7"),
                        Count = amount,
                    });    //加入附件
                    _SyncCommandManager.Handle(commandMail);
                }
                #endregion 第三封

            }
        }
    }
}
