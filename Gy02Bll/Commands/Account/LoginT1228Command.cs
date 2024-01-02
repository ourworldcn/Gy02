using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class LoginT1228Command : SyncCommandBase
    {
        /// <summary>
        /// 发行商SDK给的token。
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 返沪的登录用户对象。
        /// </summary>
        public GameUser User { get; set; }

        /// <summary>
        /// 用户的密码。首次登录才会给出。后续登录是null。
        /// </summary>
        public string Pwd { get; set; }
    }

    public class LoginT1228Handler : SyncCommandHandlerBase<LoginT1228Command>
    {
        public LoginT1228Handler(T1228Manager publisherT1228Manager, GameAccountStoreManager gameAccountStore, IDbContextFactory<GY02UserContext> dbContextFactory, SyncCommandManager syncCommandManager)
        {
            _T1228Manager = publisherT1228Manager;
            _GameAccountStore = gameAccountStore;
            _DbContextFactory = dbContextFactory;
            _SyncCommandManager = syncCommandManager;
        }

        T1228Manager _T1228Manager;
        GameAccountStoreManager _GameAccountStore;
        IDbContextFactory<GY02UserContext> _DbContextFactory;
        SyncCommandManager _SyncCommandManager;

        public override void Handle(LoginT1228Command command)
        {
            var r = _T1228Manager.GetUserInfo(token: command.Token);
            if (r.Code != 200)  //若登录失败
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = "登陆失败";
                return;
            }

            var uid = $"fs{r.Data.id}"; //用户名和密码

            //若登录成功了
            using var db = _DbContextFactory.CreateDbContext();

            var slot = db.Set<VirtualThing>().FirstOrDefault(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == uid);
            bool isCreate = false;  //是否是第一次创建用户
            string userKey = null;
            if (slot is null)    //若没有找到指定用户
            {
                //创建
                var createCommand = new CreateAccountCommand { LoginName = uid, Pwd = uid };
                _SyncCommandManager.Handle(createCommand);
                if (createCommand.HasError || createCommand.User is null)
                {
                    command.FillErrorFrom(createCommand);
                    return;
                }
                userKey = createCommand.User.Key;
                if (!_GameAccountStore.Lock(userKey))
                {
                    command.FillErrorFromWorld();
                    return;
                }
                using var dwKey = DisposeHelper.Create(c => _GameAccountStore.Unlock(c), userKey);
                if (dwKey.IsEmpty) { command.FillErrorFromWorld(); return; }

                //标记绑定关系

                var userThing = (VirtualThing)createCommand.User.Thing;
                isCreate = true;
            }
            //登录用户
            var loginCommand = new LoginCommand { LoginName = uid, Pwd = uid };
            _SyncCommandManager.Handle(loginCommand);
            if (loginCommand.HasError || loginCommand.User is null)
            {
                command.FillErrorFrom(loginCommand);
                return;
            }
            userKey = loginCommand.User.Key;
            using var dw = _GameAccountStore.GetOrLoadUser(uid, uid, out var gu);
            if (dw.IsEmpty)
            {
                command.FillErrorFromWorld();
                return;
            }
            command.User = loginCommand.User;
            if (isCreate) //若是首次创建并登录
            {
                command.Pwd = uid;
                _GameAccountStore.Save(userKey);
            }
        }
    }

}
