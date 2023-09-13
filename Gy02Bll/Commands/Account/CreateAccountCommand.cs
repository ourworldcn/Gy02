using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW;
using OW.Game.Entity;
using OW.Game.Manager;
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
            GameAccountStoreManager accountStore, GameEntityManager gameEntityManager, VirtualThingManager virtualThingManager)
        {
            _DbFactory = dbFactory;
            _LoginNameGenerator = loginNameGenerator;
            _PasswordGenerator = passwordGenerator;
            _SyncCommandManager = syncCommandManager;
            _AccountStore = accountStore;
            _GameEntityManager = gameEntityManager;
            _VirtualThingManager = virtualThingManager;
        }

        IDbContextFactory<GY02UserContext> _DbFactory;
        LoginNameGenerator _LoginNameGenerator;
        PasswordGenerator _PasswordGenerator;
        SyncCommandManager _SyncCommandManager;
        GameAccountStoreManager _AccountStore;
        GameEntityManager _GameEntityManager;
        VirtualThingManager _VirtualThingManager;

        /// <summary>
        /// 创建账号。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateAccountCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.LoginName))   //若需要生成登录名
            {
                var date = OwHelper.WorldNow;
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
#pragma warning disable CA1827 // Do not use Count() or LongCount() when Any() can be used
            //由于数据库数据特征Count优于Any
            if (db.VirtualThings.Count(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == command.LoginName) > 0)    //若指定账号已存在
            {
                command.LoginName = null;
                command.Pwd = null;
                command.ErrorCode = ErrorCodes.ERROR_USER_EXISTS;
                command.DebugMessage = $"指定账号已经存在。";
                return;
            }
#pragma warning restore CA1827 // Do not use Count() or LongCount() when Any() can be used
            //构造账号信息
            var guThing = _VirtualThingManager.Create(ProjectContent.UserTId, 1)?[0];
            if (guThing is null)
            {
                command.FillErrorFromWorld();
                return;
            }
            var gu = guThing.GetJsonObject<GameUser>();
            gu.SetDbContext(db);
            gu.LoginName = command.LoginName;
            gu.SetPwd(command.Pwd);
            db.Add(guThing);
            gu.Token = Guid.NewGuid();
            gu.Timeout = TimeSpan.FromMinutes(1);
            //加入缓存
            _AccountStore.AddUser(gu);
            command.User = gu;
            var key = gu.GetThing().IdString;
            if (_AccountStore.Lock(key))
                _SyncCommandManager.Post.Enqueue(new UnlockCommand { Key = key });
            _AccountStore.Save(guThing.IdString);

        }


    }

    public class UnlockCommand : SyncCommandBase
    {
        public object Key { get; set; }
    }

    public class UnlockHandler : SyncCommandHandlerBase<UnlockCommand>
    {
        public UnlockHandler(GameAccountStoreManager accountStore)
        {
            _AccountStore = accountStore;
        }
        GameAccountStoreManager _AccountStore;

        public override void Handle(UnlockCommand command)
        {
            _AccountStore.Unlock(command.Key);
        }
    }
}
