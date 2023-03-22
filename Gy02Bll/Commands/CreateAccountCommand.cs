using Gy02.Publisher;
using Gy02Bll.Managers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using OW;
using OW.DDD;
using OW.Game.Caching;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
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
        public CreateAccountHandler(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        /// <summary>
        /// 创建账号。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateAccountCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.LoginName))   //若需要生成登录名
            {
                var svc = _Service.GetRequiredService<LoginNameGenerator>();
                var date = DateTime.UtcNow;
                command.LoginName = svc.Generate();
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
                var svc = _Service.GetRequiredService<PasswordGenerator>();
                command.Pwd = svc.Generate(8);
            }

            var db = _Service.GetRequiredService<IDbContextFactory<GY02UserContext>>().CreateDbContext();
            if (db.VirtualThings.Any(c => c.ExtraString == command.LoginName))    //若指定账号已存在
            {
                command.ErrorCode = ErrorCodes.ERROR_USER_EXISTS;
                return;
            }
            var mng = _Service.GetRequiredService<SyncCommandManager>();
            //构造账号信息
            var commCreateUser = new CreateVirtualThingCommand() { TemplateId = ProjectContent.UserTId };
            mng.Handle(commCreateUser);

            var result = commCreateUser.Result;
            var gu = result.GetJsonObject<GameUser>();
            gu.LoginName = command.LoginName;
            gu.SetPwd(command.Pwd);
            db.Add(result);
            gu.SetDbContext(db);
            gu.Token = Guid.NewGuid();
            gu.Timeout = TimeSpan.FromMinutes(1);
            //加入缓存
            var svcStore = _Service.GetRequiredService<GameAccountStore>();
            svcStore.AddUser(gu);
            //发出创建后事件
            var commCreated = new AccountCreatedCommand() { User = gu };
            mng.Handle(commCreated);

            command.User = gu;
            svcStore.Save(result.IdString);

        }


    }
}
