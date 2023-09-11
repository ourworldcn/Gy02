using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
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
        public CharFirstLoginedHandled(GameEntityManager entityManager, GameTemplateManager templateManager)
        {
            _EntityManager = entityManager;
            _TemplateManager = templateManager;
        }

        GameEntityManager _EntityManager;
        GameTemplateManager _TemplateManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exception"></param>
        public void Handled(CharFirstLoginedCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            //fl_ExistsDayNumber 此类实体在每天第一次登录时会自动把Count置为该实体存在的总天数，从0开始。副作用，此类属实体的Count设置由系统完成单独设置无用
            var allEntityAndTemplate = _EntityManager.GetAllEntity(command.GameChar).Select(c => (Entity: c, Template: _TemplateManager.GetFullViewFromId(c.TemplateId)))
                .Where(c => c.Item2 is not null); //容错
            allEntityAndTemplate.Where(c => c.Template.Genus?.Contains(ProjectContent.ExistsDayNumberGenus) ?? false).ForEach(c =>
            {
                if (OwConvert.TryGetDateTime(c.Entity.ExtensionProperties.GetValueOrDefault("CreateDateTime"), out var dt))
                    c.Entity.Count = (now.Date - dt.Date).Days;
            });
        }

        //static void sub(string[] args)
        //{
        //    //增加累计登陆天数
        //    slot = allEntity[ProjectContent.LoginedDayTId]?.FirstOrDefault();
        //    if (slot is not null)
        //    {
        //        slot.Count++;
        //        _EntityManager.InvokeEntityChanged(new GameEntity[] { slot });
        //        _AccountStore.Save(key);
        //    }
        //}
    }
}
