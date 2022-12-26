using Microsoft.Extensions.DependencyInjection;
using OW;
using OW.DDD;
using OW.Game;
using OW.Game.Manager;
using OW.Game.Store;
using System;
using System.Collections.Generic;
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
            if (string.IsNullOrWhiteSpace(command.Pwd))
            {
                var pwdGen = _Service.GetRequiredService<PasswordGenerator>();
                command.Pwd = pwdGen.Generate(8);
            }
            CreateChar(command);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static int GetQuicklyRegisterSuffixSeq()
        {
            if (!_QuicklyRegisterSuffixSeqInit)
            {
                using var db = VWorld.CreateNewUserDbContext();
                var maxSeqStr = db.GameUsers.Where(c => c.ExtraString.StartsWith("gy")).OrderByDescending(c => c.ExtraString).FirstOrDefault()?.ExtraString ?? "0";
                var len = maxSeqStr.Reverse().TakeWhile(c => char.IsDigit(c)).Count();
                _QuicklyRegisterSuffixSeq = int.Parse(maxSeqStr[^len..^0]);
                _QuicklyRegisterSuffixSeqInit = true;
            }
            return Interlocked.Increment(ref _QuicklyRegisterSuffixSeq);
        }

        /// <summary>
        /// 创建角色对象。
        /// </summary>
        /// <param name="command"></param>
        public VirtualThing CreateChar(CreateAccountCommand command)
        {
            var result = new VirtualThing();
            return result;
        }

    }
}
