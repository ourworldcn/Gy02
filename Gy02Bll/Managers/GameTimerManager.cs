using GY02.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Managers
{
    public class GameTimerManagerOptions : IOptions<GameTimerManagerOptions>
    {
        public GameTimerManagerOptions Value => this;
    }

    /// <summary>
    /// 定时任务管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class GameTimerManager : GameManagerBase<GameTimerManagerOptions, GameTimerManager>
    {
        public GameTimerManager(IOptions<GameTimerManagerOptions> options, ILogger<GameTimerManager> logger, GameAccountStoreManager accountStoreManager,
            IServiceProvider service) : base(options, logger)
        {
            _AccountStoreManager = accountStoreManager;
            _Service = service;
            Initializer();
        }

        private void Initializer()
        {
            var now = OwHelper.WorldNow;
            var tomorrow = now.Date + TimeSpan.FromDays(1);
            _Timer = new Timer(MidnightCallback, null, tomorrow - now, TimeSpan.FromDays(1));
        }

        Timer _Timer;
        private GameAccountStoreManager _AccountStoreManager;
        IServiceProvider _Service;

        protected override void Dispose(bool disposing)
        {
            _Timer?.Dispose();
            base.Dispose(disposing);
        }
        /// <summary>
        /// 午夜更新函数。
        /// </summary>
        /// <param name="state"></param>
        public void MidnightCallback(object? state)
        {
            Queue<GameUser> query = new Queue<GameUser>();
            using var scope = _Service.CreateScope();

            foreach (var item in _AccountStoreManager.Key2User)
            {
                var user = item.Value;
                if (!_AccountStoreManager.Lock(user.Key, TimeSpan.Zero))
                {
                    query.Enqueue(user);
                    continue;
                }
                try
                {
                    if (user.IsDisposed) continue;
                    Midnight(user, scope.ServiceProvider);
                }
                catch (Exception excp)
                {
                    Logger.LogWarning("无法完成用户的午夜更新，Id={id},异常：{msg}", user.Id, excp.Message);
                }
                finally
                {
                    _AccountStoreManager?.Unlock(user.Key);
                }
            }
            while (query.TryDequeue(out var user))
            {
                if (!_AccountStoreManager.Lock(user.Key))
                {
                    Logger.LogWarning("无法完成用户的午夜更新，Id={id}", user.Id);
                    continue;
                }
                try
                {
                    if (user.IsDisposed) continue;
                    Midnight(user, scope.ServiceProvider);
                }
                catch (Exception excp)
                {
                    Logger.LogWarning("无法完成用户的午夜更新，Id={id},异常：{msg}", user.Id, excp.Message);
                }
                finally
                {
                    _AccountStoreManager?.Unlock(user.Key);
                }
            }
        }

        /// <summary>
        /// 午夜更新的实质工作函数。这个函数不负责锁定对象。
        /// </summary>
        /// <param name="user"></param>
        private bool Midnight(GameUser user, IServiceProvider service)
        {
            var nowUtc = OwHelper.WorldNow;
            var gc = user.CurrentChar;
            var syncCommandManager = service.GetService<SyncCommandManager>();

            if (gc.LastLoginDateTimeUtc is null || gc.LastLoginDateTimeUtc.Value.Date < nowUtc.Date) //若是今日第一次登录
            {
                gc.LastLoginDateTimeUtc = nowUtc.Date;
                if (gc.LogineCount <= 1) gc.LogineCount++;  //避免重复发送邮件
                var subCommand = new CharFirstLoginedCommand { GameChar = gc, LoginDateTimeUtc = nowUtc };
                syncCommandManager.Handle(subCommand);
                _AccountStoreManager.Save(user.Key);
                return subCommand.HasError;
            }
            return true;
        }
    }
}
