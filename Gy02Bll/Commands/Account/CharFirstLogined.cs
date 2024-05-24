using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
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

    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<CharFirstLoginedCommand>))]
    public class CharFirstLoginedHandled : ISyncCommandHandled<CharFirstLoginedCommand>
    {
        public CharFirstLoginedHandled(GameEntityManager entityManager, GameTemplateManager templateManager, GameAchievementManager achievementManager, GameEventManager eventManager, GameMailManager mailManager, SyncCommandManager syncCommandManager)
        {
            _EntityManager = entityManager;
            _TemplateManager = templateManager;
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
            _MailManager = mailManager;
            _SyncCommandManager = syncCommandManager;
        }

        GameEntityManager _EntityManager;
        GameTemplateManager _TemplateManager;
        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;
        GameMailManager _MailManager;
        SyncCommandManager _SyncCommandManager;

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
                var commandMail = new SendMailCommand
                {
                    GameChar = command.GameChar,
                    Mail = new SendMailItem
                    {
                        Subject = "Facebook Community Guidelines",
                        Body = "Join our community platform for generous rewards\r\nDear Heroes,\r\nWelcome to \"Animals BAM BAM\"! To celebrate the official launch of this paid closed beta test, we will be distributing gift codes containing generous in-game rewards on our official community platform Facebook. After obtaining the gift code, open the game settings, click the [Redeem Code] button, and enter the gift code to claim your rewards. \r\nAdditionally, you can also get the latest official news and game guides on our official community platform, participate in various exciting activities, and share your fantastic gaming moments. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nIf you have any questions or suggestions, feel free to contact our customer service team anytime. Wish you an enjoyable gaming experience! \r\n\r\n[Animals BAM BAM] Operations Team",
                    },
                };
                commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {
                    { "Chinese", "Facebook社区指南" },
                    { "Filipino", "Mga Alituntunin ng Komunidad ng Facebook"},
                    { "Indonesian", "Pedoman Komunitas Facebook"},
                    { "Malay", "Garis Panduan Komuniti Facebook"},
                    { "Thai", "แนวทางปฏิบัติของชุมชน Facebook"},
                };
                commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {
                    { "Chinese", "加入社群平台，获取丰厚奖励\r\n亲爱的英雄，\r\n欢迎您来到《Animals BAMBAM》， 为庆祝本次付费删档测试正式启动，我们将在官方社群平台Facebook发放包含丰厚游戏奖励的礼包码，获取礼包码后打开游戏设置点击【兑换码】按钮，将礼包码输入就可以获取。\r\n此外您还可以在我们的官方社群获取官方最新资讯和游戏攻略，参加各类精彩活动，分享您的游戏精彩瞬间。\r\nFacebook：https://www.facebook.com/profile.php?id=61556765890056\r\n如果您有任何疑问或建议，欢迎随时联系我们的客服团队。祝您游戏愉快！\r\n\r\n【Animals BAMBAM】运营团队" },
                    { "Filipino", "Sumali sa ating platform ng komunidad para sa maraming mga reward.\r\nMinamahal na Mga Hero,\r\nWelcome sa \"Animals BAM BAM\" ! Para ipagdiwang ang opisyal na paglulunsad ng paid closed beta test na ito, kami ay magbabahagi ng mga gift code na naglalaman ng mga maraming in-game reward sa ating opisyal na platform ng komunidad na Facebook. Matapos makuha ang gift code, buksan ang setting ng laro, i-click ang [Redeem Code] button, at ilagay ang gift code para i-claim ang iyong mga reward. \r\nDagdag pa dito, makakakuha ka rin ng mga napapanahong opisyal na balita at mga gabay sa laro sa ating opisyal na platform ng komunidad, makilahok sa ilang nakakatuwang mga aktibidad, at magbahagi ng iyong mga sandali sa karanasan ng masayang paglalaro. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nKung may mga katanungan ka o mga mungkahi, huwag mag-atubiling makipag-ugnayan sa ating team ng coustomer service anumang oras. Hangad namin ang isang mahusay na karanasan sa paglalaro! \r\n\r\n[Animals BAM BAM] Operations Team"},
                    { "Indonesian", "Bergabunglah dengan platform sosmed dan dapatkan hadiah besar!\r\nPahlawan yang terhormat,\r\nSelamat datang di \"Animals BAMBAM\". Untuk merayakan peluncuran resmi beta berbayar penghapusan data ini, kami akan mengeluarkan kode giftpack berisi hadiah game yang melimpah di Facebook. Setelah mendapatkan kode giftpack, buka pengaturan game dan klik tombol [Kode Penukaran], masukkan kode giftpack untuk mendapatkannya. \r\nSelain itu, kamu juga bisa mendapatkan informasi resmi dan strategi permainan terkini di komunitas resmi kami, mengikuti berbagai event menarik, serta berbagi momen bermainmu yang menyenangkan. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nHubungi tim layanan pelanggan kami jika memiliki pertanyaan atau saran. Selamat bermain! \r\n\r\nTim operasional [Animals BAMBAM]"},
                    { "Malay", "Sertai platform sosial, dapatkan ganjaran hebat\r\nWira yang dihormati,\r\nSelamat datang ke \"Animals BAM BAM\", untuk meraikan pelancaran rasmi ujian beta berbayar ini, kami akan mengedarkan kod hadiah yang mengandungi ganjaran permainan kaya di platform sosial rasmi Facebook, selepas mendapatkan kod hadiah, buka tetapan permainan dan klik butang [Tebus Kod], masukkan kod hadiah untuk mendapatkannya. \r\nDi samping itu, anda juga boleh mendapatkan berita rasmi dan petua permainan terkini dalam komuniti rasmi kami, anda juga berpeluang untuk mengambil bahagian dalam pelbagai aktiviti menarik dan boleh berkongsi momen menarik permainan anda. \r\nFacebook: https://www.facebook.com/profile.php?id=61556765890056\r\nSekiranya anda mempunyai sebarang pertanyaan atau cadangan, sila hubungi pasukan khidmat pelanggan kami. Selamat bermain! \r\n\r\nPasukan Operasi \"Animals BAM BAM\""},
                    { "Thai", "เข้าร่วมแพลตฟอร์มโซเชียล จะได้รับรางวัลจำนวนมาก\r\nสวัสดี ฮีโร่\r\nขอต้อนรับสู่ \"Animals BAMBAM\" เพื่อฉลองเปิดให้ทดสอบลบไฟล์แบบชำระเงินอย่างเป็นทางการในครั้งนี้ เราจะแจกโค้ดแลกที่มีรางวัลเกมจำนวนมากที่ Facebook ทางการของเรา หลังจากรับโค้ดแลกแล้ว เปิดหน้าตั้งค่าของเกมและกด [โค้ดแลก] จากนั้นใส่โค้ดแลกคุณก็จะสามารถรับได้ \r\nนอกจากนี้ คุณยังสามารถรับข่าวสารเกมล่าสุดและกลยุทธ์เกมได้ที่กลุ่ม Facebook ของเรา แถมยังมีกิจกรรมที่น่าตื่นเต้นอีกมากมาย มาแบ่งปันช่วงเวลาการเล่นเกมของคุณกัน \r\nFacebook：https://www.facebook.com/profile.php?id=61556765890056\r\nหากคุณมีข้อสงสัยหรือคำแนะนำใด ๆ โปรดติดต่อฝ่ายบริการลูกค้าของเราได้ตลอดเวลา ขอให้สนุกกับการเล่นเกมนะ! \r\n\r\nทีมงาน [Animals BAMBAM]"},
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("a45b3421-3688-43c5-b8f5-429db7621f69"),
                    Count = 10000,
                });     //加入附件
                //_SyncCommandManager.Handle(commandMail);
                //第二封
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
                }); ;    //加入附件
                _SyncCommandManager.Handle(commandMail);

                //第三封
                commandMail = new SendMailCommand
                {
                    GameChar = command.GameChar,
                    Mail = new SendMailItem
                    {
                        Subject = "Discord Community Guide",
                        Body = "Join our community platform for generous rewards\r\nDear Heroes,\r\nWelcome to \"Animals BAM BAM\"! To celebrate the official launch of this paid closed beta test, we will be distributing gift codes containing generous in-game rewards on our official community platform Discord. After obtaining the gift code, open the game settings, click the [Redeem Code] button, and enter the gift code to claim your rewards. \r\nAdditionally, you can also get the latest official news and game guides on our official community platform, participate in various exciting activities, and share your fantastic gaming moments. \r\nDiscord:https://discord.gg/kQAZ7AWf\r\nIf you have any questions or suggestions, feel free to contact our customer service team anytime. Wish you an enjoyable gaming experience! \r\n\r\n[Animals BAM BAM] Operations Team",
                    },
                };
                commandMail.Mail.Dictionary1 = new Dictionary<string, string>() {
                    { "Chinese", "Discord社区指南" },
                    { "Filipino", "Discord Community Guide"},
                    { "Indonesian", "Panduan Komunitas Perselisihan"},
                    { "Malay", "Panduan Komuniti Discord"},
                    { "Thai", "คู่มือชุมชน Discord"},
                };
                commandMail.Mail.Dictionary2 = new Dictionary<string, string>() {
                    { "Chinese", "加入社群平台，获取丰厚奖励\r\n亲爱的英雄，\r\n欢迎您来到《Animals BAMBAM》， 为庆祝本次付费删档测试正式启动，我们将在官方社群平台Discord发放包含丰厚游戏奖励的礼包码，获取礼包码后打开游戏设置点击【兑换码】按钮，将礼包码输入就可以获取。\r\n此外您还可以在我们的官方社群获取官方最新资讯和游戏攻略，参加各类精彩活动，分享您的游戏精彩瞬间。\r\nDiscord：https://discord.gg/kQAZ7AWf\r\n如果您有任何疑问或建议，欢迎随时联系我们的客服团队。祝您游戏愉快！\r\n\r\n【Animals BAMBAM】运营团队" },
                    { "Filipino", "Sumali sa ating platform ng komunidad para sa maraming mga reward.\r\nMinamahal na Mga Hero,\r\nWelcome sa \"Animals BAM BAM\"! Para ipagdiwang ang opisyal na paglulunsad ng paid closed beta test na ito, kami ay magbabahagi ng mga gift code na naglalaman ng mga maraming in-game reward sa ating opisyal na platform ng komunidad na Discord. Matapos makuha ang gift code, buksan ang setting ng laro, i-click ang [Redeem Code] button, at ilagay ang gift code para i-claim ang iyong mga reward. \r\nDagdag pa dito, makakakuha ka rin ng mga napapanahong opisyal na balita at mga gabay sa laro sa ating opisyal na platform ng komunidad, makilahok sa ilang nakakatuwang mga aktibidad, at magbahagi ng iyong mga sandali sa karanasan ng masayang paglalaro. \r\nDiscord:https://discord.gg/kQAZ7AWf\r\nKung may mga katanungan ka o mga mungkahi, huwag mag-atubiling makipag-ugnayan sa ating team ng coustomer service anumang oras. Hangad namin ang isang mahusay na karanasan sa paglalaro! \r\n\r\n[Animals BAM BAM] Operations Team"},
                    { "Indonesian", "Bergabunglah dengan platform sosmed dan dapatkan hadiah besar!\r\nPahlawan yang terhormat,\r\nSelamat datang di \"Animals BAMBAM\". Untuk merayakan peluncuran resmi beta berbayar penghapusan data ini, kami akan mengeluarkan kode giftpack berisi hadiah game yang melimpah di Discord. Setelah mendapatkan kode giftpack, buka pengaturan game dan klik tombol [Kode Penukaran], masukkan kode giftpack untuk mendapatkannya. \r\nSelain itu, kamu juga bisa mendapatkan informasi resmi dan strategi permainan terkini di komunitas resmi kami, mengikuti berbagai event menarik, serta berbagi momen bermainmu yang menyenangkan. \r\nDiscord: https://discord.gg/kQAZ7AWf\r\nHubungi tim layanan pelanggan kami jika memiliki pertanyaan atau saran. Selamat bermain! \r\n\r\nTim operasional [Animals BAMBAM]"},
                    { "Malay", "Sertai platform sosial, dapatkan ganjaran hebat\r\nWira yang dihormati,\r\nSelamat datang ke \"Animals BAM BAM\", untuk meraikan pelancaran rasmi ujian beta berbayar ini, kami akan mengedarkan kod hadiah yang mengandungi ganjaran permainan kaya di platform sosial rasmi Discord, selepas mendapatkan kod hadiah, buka tetapan permainan dan klik butang [Tebus Kod], masukkan kod hadiah untuk mendapatkannya. \r\nDi samping itu, anda juga boleh mendapatkan berita rasmi dan petua permainan terkini dalam komuniti rasmi kami, anda juga berpeluang untuk mengambil bahagian dalam pelbagai aktiviti menarik dan boleh berkongsi momen menarik permainan anda. \r\nDiscord:https://discord.gg/kQAZ7AWf\r\nSekiranya anda mempunyai sebarang pertanyaan atau cadangan, sila hubungi pasukan khidmat pelanggan kami. Selamat bermain! \r\n\r\nPasukan Operasi \"Animals BAM BAM\""},
                    { "Thai", "เข้าร่วมแพลตฟอร์มโซเชียล จะได้รับรางวัลมากมาย\r\nสวัสดี ฮีโร่\r\nขอต้อนรับสู่ \"Animals BAMBAM\" เพื่อฉลองเปิดให้ทดสอบลบไฟล์แบบชำระเงินอย่างเป็นทางการในครั้งนี้ เราจะแจกโค้ดแลกที่มีรางวัลเกมจำนวนมากที่ Discord ทางการของเรา หลังจากรับโค้ดแลกแล้ว เปิดหน้าตั้งค่าของเกมและกด [โค้ดแลก] จากนั้นใส่โค้ดแลกคุณก็จะสามารถรับได้ \r\nนอกจากนี้ คุณยังสามารถรับข่าวสารเกมล่าสุดและกลยุทธ์เกมได้ที่กลุ่ม Facebook ของเรา แถมยังมีกิจกรรมที่น่าตื่นเต้นอีกมากมาย มาแบ่งปันช่วงเวลาการเล่นเกมของคุณกัน \r\nDiscord: https://discord.gg/kQAZ7AWf\r\nหากคุณมีข้อสงสัยหรือคำแนะนำใด ๆ โปรดติดต่อฝ่ายบริการลูกค้าของเราได้ตลอดเวลา ขอให้สนุกกับการเล่นเกมนะ! \r\n\r\nทีมงาน [Animals BAMBAM]"},
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("a45b3421-3688-43c5-b8f5-429db7621f69"),
                    Count = 10000,
                }); ;    //加入附件
                //_SyncCommandManager.Handle(commandMail);

            }
        }
    }
}
