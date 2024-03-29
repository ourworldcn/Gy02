﻿using GY02.Base;
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
            /*AllChars	English	Welcome to the open beta test	"Dear Hero,
            Congratulations on your participation in Open Beta!
            Take up weapons and join the battle in a world full of monsters! Fight with every enemy!
            Feel free to provide us with feedback as we continue to improve the gaming experience.
            The adventure will begin soon!"	110101001	100	30	新建玩家邮件
            */
            if (command.GameChar.LogineCount <= 1)  //若第一次登录
            {
                var commandMail = new SendMailCommand
                {
                    GameChar = command.GameChar,
                    Mail = new SendMailItem
                    {
                        Subject = "Welcome to the open beta test",
                        Body = "Dear Hero,\r\n\r\nCongratulations on your participation in Open Beta!\r\n\r\nTake up weapons and join the battle in a world full of monsters! Fight with every enemy!\r\n\r\nFeel free to provide us with feedback as we continue to improve the gaming experience.\r\n\r\nThe adventure will begin soon!",
                    },
                };
                commandMail.ToIds.Add(command.GameChar.Id);   //加入收件人
                commandMail.Mail.Attachment.Add(new Templates.GameEntitySummary
                {
                    TId = Guid.Parse("c9575f24-a33d-49ba-b130-29b6ff4d62c7"),
                    Count = 100,
                }); ;    //加入附件
                _SyncCommandManager.Handle(commandMail);
            }
        }
    }
}
