﻿using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System.Diagnostics.CodeAnalysis;

namespace GY02.Commands
{
    /// <summary>
    /// 账号被创建后的事件。
    /// </summary>
    public class CreateAccountCommand : SyncCommandBase
    {
        public CreateAccountCommand()
        {
        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。可省略，则自动指定。
        /// </summary>
        [AllowNull]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码，可省略，则自动指定。
        /// </summary>
        [AllowNull]
        public string Pwd { get; set; }

        #endregion 可映射属性

        /// <summary>
        /// 返回创建的新用户账号。
        /// </summary>
        public GameUser User { get; set; }
    }

    public class CreateAccountHandler : SyncCommandHandlerBase<CreateAccountCommand>
    {
        public CreateAccountHandler(IDbContextFactory<GY02UserContext> dbFactory, LoginNameGenerator loginNameGenerator, PasswordGenerator passwordGenerator, SyncCommandManager syncCommandManager,
            GameAccountStore accountStore)
        {
            _DbFactory = dbFactory;
            _LoginNameGenerator = loginNameGenerator;
            _PasswordGenerator = passwordGenerator;
            _SyncCommandManager = syncCommandManager;
            _AccountStore = accountStore;
        }

        IDbContextFactory<GY02UserContext> _DbFactory;
        LoginNameGenerator _LoginNameGenerator;
        PasswordGenerator _PasswordGenerator;
        SyncCommandManager _SyncCommandManager;
        GameAccountStore _AccountStore;

        /// <summary>
        /// 创建账号。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateAccountCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.LoginName))   //若需要生成登录名
            {
                var date = DateTime.UtcNow;
                command.LoginName = _LoginNameGenerator.Generate();
            }
            using var dwLoginName = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, command.LoginName, TimeSpan.FromSeconds(2));   //锁定登录名
            if (dwLoginName.IsEmpty)    //若无法锁定登录名
            {
                command.HasError = true;
                command.ErrorCode = 258;
                return;
            }
            if (string.IsNullOrEmpty(command.Pwd))  //若需要生成密码
            {
                command.Pwd = _PasswordGenerator.Generate(8);
            }

            var db = _DbFactory.CreateDbContext();
            if (db.VirtualThings.Any(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == command.LoginName))    //若指定账号已存在
            {
                command.ErrorCode = ErrorCodes.ERROR_USER_EXISTS;
                command.DebugMessage = $"指定账号已经存在。";
                return;
            }
            //构造账号信息
            var commCreateUser = new CreateVirtualThingCommand() { TemplateId = ProjectContent.UserTId };
            _SyncCommandManager.Handle(commCreateUser);

            var result = commCreateUser.Result;
            var gu = result.GetJsonObject<GameUser>();
            gu.LoginName = command.LoginName;
            gu.SetPwd(command.Pwd);
            db.Add(result);
            gu.SetDbContext(db);
            gu.Token = Guid.NewGuid();
            gu.Timeout = TimeSpan.FromMinutes(1);
            //加入缓存
            _AccountStore.AddUser(gu);
            //发出创建后事件
            var commCreated = new AccountCreatedCommand() { User = gu };
            _SyncCommandManager.Handle(commCreated);

            command.User = gu;
            _AccountStore.Save(result.IdString);

        }


    }
}
