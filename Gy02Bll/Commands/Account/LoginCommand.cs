using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using GY02.Commands;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Store;
using OW.GameDb;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class LoginCommand : SyncCommandBase
    {
        public LoginCommand()
        {

        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }

        #endregion 可映射属性

        /// <summary>
        /// 成功时返回的登录的用户对象。
        /// </summary>
        public GameUser User { get; set; }
    }

    public class LoginHandler : SyncCommandHandlerBase<LoginCommand>
    {

        public LoginHandler(IServiceProvider service, GameSqlLoggingManager loggingManager, SyncCommandManager syncCommandManager)
        {
            _Service = service;
            _LoggingManager = loggingManager;
            _SyncCommandManager = syncCommandManager;
        }

        IServiceProvider _Service;
        GameSqlLoggingManager _LoggingManager;
        SyncCommandManager _SyncCommandManager;

        public override void Handle(LoginCommand command)
        {
            var svcStore = _Service.GetRequiredService<GameAccountStore>();
            var exists = svcStore.LoginName2Key.ContainsKey(command.LoginName);  //是否已经登录
            if (!svcStore.LoadOrGetUser(command.LoginName, command.Pwd, out var gu))
            {
                command.FillErrorFromWorld();
                return;
            }
            using var dw = DisposeHelper.Create(svcStore.Lock, svcStore.Unlock, gu.Key, TimeSpan.FromSeconds(3));
            if (dw.IsEmpty || gu.IsDisposed)
            {
                command.FillErrorFromWorld();
                return;
            }
            var nowUtc = DateTime.UtcNow;
            command.User = gu;
            //设置属性
            gu.Timeout = TimeSpan.FromMinutes(15);
            gu.LastModifyDateTimeUtc = DateTime.UtcNow;
            if (exists)  //若是重新登录
            {
                if (!svcStore.ChangeToken(gu, Guid.NewGuid()))
                {
                    command.FillErrorFromWorld();
                    return;
                }
            }
            var db = gu.GetDbContext();
            gu.CurrentChar = ((VirtualThing)gu.Thing).Children.First(c => c.ExtraGuid == ProjectContent.CharTId).GetJsonObject<GameChar>();
            gu.CurrentChar.LogineCount++;
            _LoggingManager.AddLogging(new GameActionRecord { ActionId = "Loginged", ParentId = gu.Id });
            var gc = gu.CurrentChar;
            if (gc.LastLoginDateTimeUtc is null || gc.LastLoginDateTimeUtc.Value.Date < nowUtc.Date) //若是今日第一次登录
            {
                gc.LastLoginDateTimeUtc = nowUtc.Date;
                var subCommand = new CharFirstLoginedCommand { GameChar = gc, LoginDateTimeUtc = nowUtc };
                _SyncCommandManager.Handle(subCommand);
                svcStore.Save(gu.Key);
            }
        }

    }
}
