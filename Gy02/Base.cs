using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions.Options;
using OW;
using OW.Game.Manager;
using OW.SyncCommand;
using System;
using System.Diagnostics;

namespace GY02
{
    /// <summary>
    /// 
    /// </summary>
	public static class ReturnDtoExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="src"></param>
        public static void FillErrorFrom(this ReturnDtoBase obj, SyncCommandBase src)
        {
            obj.ErrorCode = src.ErrorCode;
            obj.DebugMessage = src.DebugMessage;
            obj.HasError = src.HasError;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public static void FillErrorFromWorld(this ReturnDtoBase obj)
        {
            obj.ErrorCode = OwHelper.GetLastError();
            obj.DebugMessage = OwHelper.GetLastErrorMessage();
            obj.HasError = 0 != obj.ErrorCode;

        }


    }

    /// <summary>
    /// <see cref="UdpScanner"/>的配置类。
    /// </summary>
    public class UdpScannerOptions : IOptions<UdpScannerOptions>
    {
        /// <summary>
        /// 
        /// </summary>
        public UdpScannerOptions Value => this;
    }

    /// <summary>
    /// 分发变化数据的服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class UdpScanner : OwServiceBase<UdpScannerOptions, UdpScanner>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="udpServerManager"></param>
        /// <param name="gameAccountStore"></param>
        /// <param name="hostLifetime"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public UdpScanner(UdpServerManager udpServerManager, GameAccountStore gameAccountStore, IHostApplicationLifetime hostLifetime, IOptions<UdpScannerOptions> options, ILogger<UdpScanner> logger) : base(options, logger)
        {
            _UdpServerManager = udpServerManager;
            _GameAccountStore = gameAccountStore;
            _Timer = new Timer(c => UdpTimer(), null, 60_000, 60_000);
            _HostLifetime = hostLifetime;
        }

        UdpServerManager _UdpServerManager;
        GameAccountStore _GameAccountStore;
        IHostApplicationLifetime _HostLifetime;
        Timer? _Timer;

        /// <summary>
        /// 分发数据变化的函数。
        /// </summary>
        public void UdpTimer()
        {
            if (_UdpServerManager is null)
                return;

            foreach (var item in _GameAccountStore._Key2User)
            {
                if (_HostLifetime.ApplicationStopping.IsCancellationRequested)
                    return;
                if (!_GameAccountStore.Lock(item.Key, TimeSpan.Zero))
                    continue;
                using var dw = DisposeHelper.Create(c => _GameAccountStore.Unlock(c), item.Key);
                Debug.Assert(!dw.IsEmpty);
                var tili = item.Value.CurrentChar.HuoBiSlot.Children.Single(c => c.TemplateId == ProjectContent.PowerTId);
                if (tili is null)
                    continue;
                var fcp = tili.Fcps.GetValueOrDefault(nameof(tili.Count));
                if (fcp is null)
                    continue;
                var oVal = fcp.CurrentValue;
                var nVal = fcp.GetCurrentValueWithUtc();

                if (oVal != nVal) //若已经变化
                    _UdpServerManager.SendObject(item.Value.Token, new GamePropertyChangeItemDto
                    {
                        DateTimeUtc = OwHelper.WorldClock,
                        ObjectId = tili.Id,
                        TId = tili.TemplateId,
                        PropertyName = nameof(tili.Count),

                        HasOldValue = true,
                        OldValue = oVal,
                        HasNewValue = true,
                        NewValue = nVal,
                    });
                //孵化次数
                var cishu = item.Value.CurrentChar.HuoBiSlot.Children.FirstOrDefault(c => c.TemplateId == ProjectContent.FuhuaCishuTId);
                if (cishu is null)
                    continue;
                var fcpCishu = cishu.Fcps.GetValueOrDefault(nameof(cishu.Count));
                if (fcpCishu is null)
                    continue;
                oVal = fcpCishu.CurrentValue;
                nVal = fcpCishu.GetCurrentValueWithUtc();
                if (oVal != nVal) //若已经变化
                {
                    _UdpServerManager.SendObject(item.Value.Token, new GamePropertyChangeItemDto
                    {
                        DateTimeUtc = OwHelper.WorldClock,
                        ObjectId = cishu.Id,
                        TId = cishu.TemplateId,
                        PropertyName = nameof(cishu.Count),

                        HasOldValue = true,
                        OldValue = oVal,
                        HasNewValue = true,
                        NewValue = nVal,
                    });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _Timer?.Dispose();
                _Timer = null;
            }
            base.Dispose(disposing);
        }
    }
}