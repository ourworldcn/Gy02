using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Gy02Bll.Commands.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.SyncCommand;

namespace Gy02.Controllers
{
    /// <summary>
    /// 邮件控制器。
    /// </summary>
    public class MailController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public MailController(GameMailManager mailManager, GameAccountStore gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager)
        {
            _MailManager = mailManager;
            _GameAccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
        }

        GameMailManager _MailManager;
        GameAccountStore _GameAccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;

        /// <summary>
        /// 发送邮件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<SendMailReturnDto> SendMail(SendMailParamsDto model)
        {
            var result = new SendMailReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new SendMailCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 获取指定用户收件箱中的邮件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetMailsReturnDto> GetMails(GetMailsParamsDto model)
        {
            var result = new GetMailsReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetMailsCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }
    }

}
