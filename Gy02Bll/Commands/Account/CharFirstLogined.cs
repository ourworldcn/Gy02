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
                        Subject = "撞飞一切！",
                        Body = "Hi heroes,\r\nTo celebrate the official open beta of \"Animals BAM BAM\" today, and to thank you for your enthusiasm and support, we hereby offer you [100 diamonds]. \r\nWish all heroes an enjoyable gaming experience!",
                    },
                };
                commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {   //多语言标题
                    { "English", "Let's Get Bumping!" },
                    { "Chinese", "撞飞一切！"},
                    //{ "Filipino", "Recharge Rebate Email"},
                    //{ "Indonesian", "Email Rabat Isi Ulang"},
                    //{ "Malay", "Mengisi semula e -mel rebat"},
                    //{ "Thai", "เติมเงินคืนอีเมล"},
                };
                commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {   //多语言正文
                    { "English", "Hero! Welcome to Bump Bump survivor! Bump over everything! If you need assistance, tap the menu on the top left corner and select Help to contact customer service!" },
                    { "Chinese", "勇士！欢迎来到Bump Bump survivor！尽情的撞翻一切吧！如需帮助，可点击左上角列表-Help获取专业人员协助！"},
                    //{ "Filipino", "Hi hero,\r\nIto ang iyong rebate ng recharge, mangyaring bigyang -pansin upang suriin."},
                    //{ "Indonesian", "Hai Pahlawan,\r\nIni adalah rabat isi ulang Anda, harap perhatikan untuk memeriksa."},
                    //{ "Malay", "Hai wira,\r\nIni adalah rebat cas semula anda, sila perhatikan untuk diperiksa."},
                    //{ "Thai", "สวัสดีฮีโร่\r\nนี่คือการคืนเงินเติมเงินของคุณโปรดใส่ใจในการตรวจสอบ"},
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("c9575f24-a33d-49ba-b130-29b6ff4d62c7"),   //钻石
                    Count = 100,
                });     //加入附件
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("a45b3421-3688-43c5-b8f5-429db7621f69"),   //金币
                    Count = 3000,
                });     //加入附件
                _SyncCommandManager.Handle(commandMail);
                #endregion 第一封

                #region 第二封
                commandMail = new SendMailCommand
                {
                    GameChar = command.GameChar,
                    Mail = new SendMailItem
                    {
                        Subject = "Facebook/Discord Community Guidelines",
                        Body = "Dear Heroes,\r\nTo celebrate the successful open beta of \"Animals BAMBAM\", we will be distributing gift codes containing generous in-game rewards on our official community platform Facebook/Discord. After obtaining the gift code, open the game settings, click the [Redeem Code] button, and enter the gift code to claim your rewards. \r\nAdditionally, you can also get the latest official news and game guides on our official community platform, participate in various exciting activities, and share your fantastic gaming moments. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nDiscord:\r\nhttps://discord.gg/TtYePkUp\r\nIf you have any questions or suggestions, feel free to contact our customer service team anytime. Wish you an enjoyable gaming experience! \r\n\r\n[Animals BAM BAM] Operations Team",
                    },
                };
                commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {
                    { "English", "Facebook/Discord Community Guidelines" },
                    { "Chinese", "Facebook/Discord社区指南"},
                    { "Filipino", "Mga Alituntunin ng Komunidad ng Facebook/Discord"},
                    { "Indonesian", "Pedoman Komunitas Facebook/Discord\r\nBergabunglah dengan platform sosmed dan dapatkan hadiah besar!"},
                    { "Malay", "Garis Panduan Komuniti Facebook/Discord"},
                    { "Thai", "แนวทางปฏิบัติของชุมชน Facebook/Discord\r\nเข้าร่วมแพลตฟอร์มชุมชนของเราเพื่อรับรางวัลมากมาย"},
                };
                commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {
                    { "English", "Dear Heroes,\r\nTo celebrate the successful open beta of \"Animals BAMBAM\", we will be distributing gift codes containing generous in-game rewards on our official community platform Facebook/Discord. After obtaining the gift code, open the game settings, click the [Redeem Code] button, and enter the gift code to claim your rewards. \r\nAdditionally, you can also get the latest official news and game guides on our official community platform, participate in various exciting activities, and share your fantastic gaming moments. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nDiscord:\r\nhttps://discord.gg/TtYePkUp\r\nIf you have any questions or suggestions, feel free to contact our customer service team anytime. Wish you an enjoyable gaming experience! \r\n\r\n[Animals BAM BAM] Operations Team" },
                    { "Chinese", "亲爱的英雄，\r\n为庆祝《Animals BAMBAM》顺利公测，我们将在官方社群平台Facebook/Discord发放包含丰厚游戏奖励的礼包码，获取礼包码后打开游戏设置点击【兑换码】按钮，将礼包码输入就可以获取。\r\n此外您还可以在我们的官方社群获取官方最新资讯和游戏攻略，参加各类精彩活动，分享您的游戏精彩瞬间。\r\nFacebook：https://www.facebook.com/profile.php?id=61556765890056\r\nDiscord:\r\nhttps://discord.gg/TtYePkUp\r\n如果您有任何疑问或建议，欢迎随时联系我们的客服团队。祝您游戏愉快！\r\n\r\n【Animals BAMBAM】运营团队"},
                    { "Filipino", "Sumali sa ating platform ng komunidad para sa maraming mga reward.\r\nMinamahal na Mga Hero,\r\nUpang ipagdiwang ang matagumpay na open beta ng \"Animals BAMBAM\", kami ay magbabahagi ng mga gift code na naglalaman ng mga maraming in-game reward sa ating opisyal na platform ng komunidad na Facebook/Discord. Matapos makuha ang gift code, buksan ang setting ng laro, i-click ang [Redeem Code] button, at ilagay ang gift code para i-claim ang iyong mga reward. \r\nDagdag pa dito, makakakuha ka rin ng mga napapanahong opisyal na balita at mga gabay sa laro sa ating opisyal na platform ng komunidad, makilahok sa ilang nakakatuwang mga aktibidad, at magbahagi ng iyong mga sandali sa karanasan ng masayang paglalaro. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nDiscord:\r\nhttps://discord.gg/TtYePkUp\r\nKung may mga katanungan ka o mga mungkahi, huwag mag-atubiling makipag-ugnayan sa ating team ng coustomer service anumang oras. Hangad namin ang isang mahusay na karanasan sa paglalaro! \r\n\r\n[Animals BAM BAM] Operations Team"},
                    { "Malay", "Wira yang dihormati,\r\nUntuk meraikan kejayaan beta terbuka \"Animals BAMBAM\", kami akan mengedarkan kod hadiah yang mengandungi ganjaran permainan kaya di platform sosial rasmi Facebook/Discord, selepas mendapatkan kod hadiah, buka tetapan permainan dan klik butang [Tebus Kod], masukkan kod hadiah untuk mendapatkannya. \r\nDi samping itu, anda juga boleh mendapatkan berita rasmi dan petua permainan terkini dalam komuniti rasmi kami, anda juga berpeluang untuk mengambil bahagian dalam pelbagai aktiviti menarik dan boleh berkongsi momen menarik permainan anda. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nSekiranya anda mempunyai sebarang pertanyaan atau cadangan, sila hubungi pasukan khidmat pelanggan kami. Selamat bermain! \r\n\r\nPasukan Operasi \"Animals BAM BAM\""},
                    { "Thai", "เรียนวีรบุรุษ\r\nเพื่อเป็นการเฉลิมฉลองโอเพ่นเบต้าที่ประสบความสำเร็จของ \"Animals BAMBAM\" เราจะแจกรหัสของขวัญที่ประกอบด้วยรางวัลมากมายในเกมบนแพลตฟอร์มชุมชนอย่างเป็นทางการของเรา Facebook/Discord หลังจากได้รับรหัสของขวัญแล้ว ให้เปิดการตั้งค่าเกม คลิกปุ่ม [แลกรหัส] และป้อนรหัสของขวัญเพื่อรับรางวัลของคุณ \r\nนอกจากนี้คุณยังสามารถรับข่าวสารอย่างเป็นทางการล่าสุดและคำแนะนำเกมบนแพลตฟอร์มชุมชนอย่างเป็นทางการของเรา เข้าร่วมในกิจกรรมที่น่าตื่นเต้นมากมาย และแบ่งปันช่วงเวลาการเล่นเกมที่ยอดเยี่ยมของคุณ \r\nเฟสบุ๊ค: https://www.facebook.com/profile.php?id=61556765890056\r\nความไม่ลงรอยกัน:\r\nhttps://discord.gg/TtYePkUp\r\nหากคุณมีคำถามหรือข้อเสนอแนะ โปรดติดต่อทีมบริการลูกค้าของเราได้ตลอดเวลา หวังว่าคุณจะได้รับประสบการณ์การเล่นเกมที่สนุกสนาน! \r\n\r\n[แอนิมอลแบมแบม] "},
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("a45b3421-3688-43c5-b8f5-429db7621f69"),
                    Count = 10000,
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
                    //_SyncCommandManager.Handle(commandMail);
                }
                #endregion 第三封

            }
        }
    }
}
