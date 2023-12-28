using AutoMapper;
using GY02;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OW.SyncCommand;
using System.Text.Json.Serialization;

namespace Gy02.Controllers
{
    /// <summary>
    /// T1228合作伙伴相关功能的控制器。
    /// </summary>
    public class T1228Controller : GameControllerBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public T1228Controller(IMapper mapper, SyncCommandManager syncCommandManager)
        {
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
        }

        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;

        /// <summary>
        /// 支付回调接口。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<Payed1228ReturnDto> Payed1228(Payed1228ParamsDto model)
        {
            var result = new Payed1228ReturnDto();
            return result;
        }
    }

}
