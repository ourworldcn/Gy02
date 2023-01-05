using Gy02.Publisher;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OW;
using OW.DDD;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class CreateAccountCommand : GameCommandBase
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
    }

    public class CreateAccountHandler : GameCommandHandlerBase<CreateAccountCommand>
    {
        private static bool _QuicklyRegisterSuffixSeqInit;
        private static int _QuicklyRegisterSuffixSeq;

        public CreateAccountHandler(IServiceProvider service)
        {
            _Service = service;
        }

        public IServiceProvider _Service { get; set; }

        /// <summary>
        /// 创建账号。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateAccountCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.LoginName))
            {
                var date = DateTime.UtcNow;
                command.LoginName = $"gy{GetQuicklyRegisterSuffixSeq()}";
            }
            using var dwLoginName = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, command.LoginName, TimeSpan.FromSeconds(2));   //锁定登录名
            
            if (dwLoginName.IsEmpty)
            {
                command.HasError = true;
                command.ErrorCode = 258;
                return;
            }
            if (string.IsNullOrWhiteSpace(command.Pwd))
            {
                var pwdGen = _Service.GetRequiredService<PasswordGenerator>();
                command.Pwd = pwdGen.Generate(8);
            }
            var gcm = _Service.GetRequiredService<GameCommandManager>();
            var commandCvt = new CreateVirtualThingCommand()
            {
                TemplateId = ProjectContent.CharTId,
            };
            gcm.Handle(commandCvt);
            command.FillErrorFrom(commandCvt);
            if (!command.HasError)   //若成功创建
            {
                var result = commandCvt.Result;
                var cache = _Service.GetRequiredService<GameObjectCache>();
                //加入缓存
                var db = result.RuntimeProperties.GetOrAdd("DbContext", c => VWorld.CreateNewUserDbContext());
                var gc = result.GetJsonObject<GameChar>();
                cache.Set(result.IdString, result);
                //构造账号信息

            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static int GetQuicklyRegisterSuffixSeq()
        {
            if (!_QuicklyRegisterSuffixSeqInit)
            {
                using var db = VWorld.CreateNewUserDbContext();
                var maxSeqStr = db.OrphanedThings.Where(c => c.ExtraString.StartsWith("gy")).OrderByDescending(c => c.ExtraString).FirstOrDefault()?.ExtraString ?? "0";
                var len = maxSeqStr.Reverse().TakeWhile(c => char.IsDigit(c)).Count();
                _QuicklyRegisterSuffixSeq = int.Parse(maxSeqStr[^len..^0]);
                _QuicklyRegisterSuffixSeqInit = true;
            }
            return Interlocked.Increment(ref _QuicklyRegisterSuffixSeq);
        }

    }
}
