using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands.Account
{
    public class LoginT0314Command : SyncCommandBase
    {
        public LoginT0314Command()
        {

        }

        /// <summary>
        /// 发行商SDK给的token。
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 发行商给的Uid。
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// 返沪的登录用户对象。
        /// </summary>
        public GameUser User { get; set; }

        /// <summary>
        /// 用户的密码。首次登录才会给出。后续登录是null。
        /// </summary>
        public string Pwd { get; set; }
    }

    public class LoginT0314Handler : SyncCommandHandlerBase<LoginT0314Command>
    {
        public LoginT0314Handler(T0314Manager t0314Manager, GameAccountStoreManager gameAccountStore, IDbContextFactory<GY02UserContext> dbContextFactory, SyncCommandManager syncCommandManager)
        {
            _T0314Manager = t0314Manager;
            _GameAccountStore = gameAccountStore;
            _DbContextFactory = dbContextFactory;
            _SyncCommandManager = syncCommandManager;
        }

        GameAccountStoreManager _GameAccountStore;
        T0314Manager _T0314Manager;
        IDbContextFactory<GY02UserContext> _DbContextFactory;
        SyncCommandManager _SyncCommandManager;

        public override void Handle(LoginT0314Command command)
        {
            var data = _T0314Manager.Login(command.Token, command.Uid);
            if (!data.Status)
            {
                command.DebugMessage = $"无法登录——{data.Message}";
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            //若登录成功了
            var uid = command.Uid;
            using var db = _DbContextFactory.CreateDbContext();

            var userThing = db.Set<VirtualThing>().SingleOrDefault(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == uid);
            bool isCreate = false;  //是否是第一次创建用户
            string userKey = null;
            if (userThing is null)    //若没有找到指定用户
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
                //var t78slot = _VirtualThingManager.Create(t78tid, 1).First();
                //var t78slot = new VirtualThing { ExtraGuid = t78tid, ExtraString = uid };

                userThing = (VirtualThing)createCommand.User.Thing;
                //VirtualThingManager.Add(t78slot, userThing);
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
            //command.ResultString = t87Result.ResultString;
            command.User = loginCommand.User;
            if (isCreate) //若是首次创建并登录
            {
                command.Pwd = uid;
                _GameAccountStore.Save(userKey);
            }
        }
    }
}
