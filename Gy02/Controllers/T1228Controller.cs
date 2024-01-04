using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GY02.Controllers
{
    /// <summary>
    /// T1228合作伙伴相关功能的控制器。
    /// </summary>
    public class T1228Controller : GameControllerBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public T1228Controller(IMapper mapper, SyncCommandManager syncCommandManager, ILogger<T1228Controller> logger, T1228Manager t1228Manager,
            GameAccountStoreManager accountStoreManager)
        {
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _Logger = logger;
            _T1228Manager = t1228Manager;
            _AccountStoreManager = accountStoreManager;
        }

        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;
        ILogger<T1228Controller> _Logger;
        T1228Manager _T1228Manager;
        GameAccountStoreManager _AccountStoreManager;

        /// <summary>
        /// 客户端密钥。
        /// </summary>
        public const string _ClientSecret = "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";

        /// <summary>
        /// 客户端ID。
        /// </summary>
        public const string _ClientId = "676765221516-0bph3evkqe602f40juesa8kekhi2ccnk.apps.googleusercontent.com";

        /// <summary>
        /// 后台秘钥。
        /// </summary>
        public const string _Key = " MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAimBobSIjfnCphlDMD6ivf0q1HgbCvqC8/1uHrTqLHKSIs1oSbhredWi/BSU6pUBiMM63eMIb98niego6JXg9jviQJjaNyaKbjM1KvM/LqFN5/kGRFRpR+kDQya8PXlm6jCxwQv1Bo6pfbUEJoiWtP9K7CGN0uOkCrvkt8ba+jL7sfVTYEo4eroyjzZCzwy3h/E5OAMQBwCebIDL7zFSa/XEA2CsoDLj0vPWSyISJVZGvz2TjQ/LxnLbdcrzJY1RSllUzXaZTuN7mv9uhqEQHVi2J52G+yPCRSzzvxlpjMlTvVq4tUp8Jdz26usq6P2dQz/Kl6wmlX7lvqbiZXeLc8wIDAQAB";

        /// <summary>
        /// AppId。
        /// </summary>
        public const string _AppId = "514540092563308819";

        /// <summary>
        /// 
        /// </summary>
        public const string _CallbackUrl = "https://sa.meetsocial.1stlightstudio.com:20443/api/T1228/Payed1228";

        /// <summary>
        /// 调试地址。
        /// </summary>
        public const string DebugUrl = "https://business.meetgames.com/tools/paymentNotice";
        /// <summary>
        /// 获取订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetT1228OrderReturnDto> GetT1228Order(GetT1228OrderParamsDto model)
        {
            var result = new GetT1228OrderReturnDto { };
            return result;
        }

        /// <summary>
        /// 支付回调接口。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<Payed1228ReturnDto> Payed1228(Payed1228ParamsDto model)
        {
            var result = new Payed1228ReturnDto();
            _Logger.LogInformation($"T1228/Payed1228收到支付确认调用，参数：{JsonSerializer.Serialize(model)}");
            var str = _T1228Manager.GetString(model);
            var md5 = _T1228Manager.GetSign(str);

            var id = Guid.NewGuid();
            _Logger.LogInformation($"T1228/Payed1228确认支付调用。id={id}");
            result.DebugMessage = $"{id}";
            //透参的格式是：角色Id,商品TId,如:2A92C88B-CF43-4C47-9B53-0832CCCBD805,D330809B-AF3C-4A71-B2ED-9D9AABED4281
            string payload = "";    //透参
            var ary = payload.Split(',');
            if (ary.Length != 2 || !Guid.TryParse(ary[0], out var gcId/*角色Id*/) || !Guid.TryParse(ary[1], out var shoppingTid/*商品Id*/))
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"透参格式错误——{payload}";
                _Logger.LogWarning(result.DebugMessage);
                result.Result = result.DebugMessage;
                return result;
            }
            var order = new GameShoppingOrder   //订单
            {
                Confirm1 = true,
                State = 0,
            };

            //using var dw = _AccountStoreManager.GetCharFromToken(model.Token, out var gc);
            //var bi = gc!.HuoBiSlot.Children.FirstOrDefault(c => c.TemplateId == ProjectContent.FabiTId);  //法币占位符
            //if (bi is null)
            //{
            //    result.DebugMessage = $"法币占位符为空。CharId={gc.Id}";
            //    result.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
            //    result.HasError = true;
            //    _Logger.LogWarning(result.DebugMessage);
            //    return result;
            //}
            //bi.Count++;
            //var command = new ShoppingBuyCommand
            //{
            //    Count = 1,
            //    GameChar = gc,
            //    ShoppingItemTId = tt.TemplateId,
            //};
            //_SyncCommandManager.Handle(command);
            //if (command.HasError)
            //{
            //    if (bi.Count > 0) bi.Count--;
            //    result.FillErrorFrom(command);
            //    _Logger.LogWarning("出现错误——{msg}", result.DebugMessage);
            //    return result;
            //}
            //_Mapper.Map(command.Changes, result.Changes);

            //order.Confirm1 = true;
            //order.Confirm2 = true;
            //order.State = 1;
            //order.CompletionDateTime = OwHelper.WorldNow;
            //try
            //{
            //    _DbContext.SaveChanges();
            //}
            //catch (Exception err)
            //{
            //    _Logger.LogWarning("保存订单号{id}的订单时出错——{msg}", order.Id, err.Message);
            //    result.DebugMessage = $"保存订单号{order.Id}的订单时出错——{err.Message}";
            //    result.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
            //    return result;
            //}
            //_Logger.LogInformation("订单号{id}已经确认成功。", order.Id);

            return result;
        }

    }

}
