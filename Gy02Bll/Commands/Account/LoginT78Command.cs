﻿using GY02.Commands;
using GY02.Managers;
using Microsoft.EntityFrameworkCore;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Store;
using OW.SyncCommand;

namespace GY02.Publisher
{
#pragma warning disable IDE1006 // 命名样式

    public class T78DataDto
    {
        public string GameId { get; set; }

        public int ChannelId { get; set; }

        public string AppId { get; set; }

        public string UserId { get; set; }

        public object SdkData { get; set; }

        public string AccessToken { get; set; }
    }

    public class T78LoginReturnDto
    {
        public T78LoginReturnDto()
        {

        }

        public string Ret { get; set; }

        public string msg { get; set; }

        public T78LoginContentDto Content { get; set; } = new T78LoginContentDto();

        public string ResultString { get; set; }
    }

    public class T78LoginContentDto
    {
        public T78DataDto Data { get; set; } = new T78DataDto();

        public object CData { get; set; }

    }

    #region 付费回调


    #endregion 付费回调

#pragma warning restore IDE1006 // 命名样式

    public class LoginT78Command : SyncCommandBase
    {
        /// <summary>
        /// 发行商SDK给的的sid。
        /// </summary>
        public string Sid { get; set; }

        /// <summary>
        /// 成功时返回的登录的用户对象。
        /// </summary>
        public GameUser User { get; set; }

        /// <summary>
        /// 密码。若首次登录，创建了账号则这里返回密码。否则返回null。
        /// </summary>
        public string Pwd { get; set; }

        public string ResultString { get; set; }

    }

    public class LoginT78Handler : SyncCommandHandlerBase<LoginT78Command>
    {
        public LoginT78Handler(PublisherT78Manager publisherT78Manager, GameAccountStoreManager gameAccountStore, IDbContextFactory<GY02UserContext> dbContextFactory, SyncCommandManager syncCommandManager)
        {
            _PublisherT78Manager = publisherT78Manager;
            _GameAccountStore = gameAccountStore;
            _DbContextFactory = dbContextFactory;
            _SyncCommandManager = syncCommandManager;
        }

        PublisherT78Manager _PublisherT78Manager;
        GameAccountStoreManager _GameAccountStore;
        IDbContextFactory<GY02UserContext> _DbContextFactory;
        SyncCommandManager _SyncCommandManager;

        public override void Handle(LoginT78Command command)
        {
            var t87Result = _PublisherT78Manager.Login(command.Sid);
            var uid = t87Result.Content.Data.UserId;
            if (t87Result.Ret != "0")   //若登录失败
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = t87Result.msg;
                return;
            }
            //若登录成功了
            using var db = _DbContextFactory.CreateDbContext();
            var t78tid = ProjectContent.T78SlotTId;    // new Guid("7A7A7058-CB88-4D54-80E9-22241774CF51");
            var slot = db.Set<VirtualThing>().SingleOrDefault(c => c.ExtraGuid == t78tid && c.ExtraString == uid);
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
                //var t78slot = _VirtualThingManager.Create(t78tid, 1).First();
                var t78slot = new VirtualThing { ExtraGuid = t78tid, ExtraString = uid };

                var userThing = (VirtualThing)createCommand.User.Thing;
                VirtualThingManager.Add(t78slot, userThing);
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
            command.ResultString = t87Result.ResultString;
            command.User = loginCommand.User;
            if (isCreate) //若是首次创建并登录
            {
                command.Pwd = uid;
                _GameAccountStore.Save(userKey);
            }
        }
    }
}
