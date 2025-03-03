using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Commands.Mail;
using GY02.Managers;
using GY02.Publisher;
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
        public MailController(GameMailManager mailManager, GameAccountStoreManager gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager)
        {
            _MailManager = mailManager;
            _GameAccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
        }

        GameMailManager _MailManager;
        GameAccountStoreManager _GameAccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;

        /// <summary>
        /// 发送邮件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>  
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

        /// <summary>
        /// 获取邮件附件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<PickUpAttachmentReturnDto> PickUpAttachment(PickUpAttachmentParamsDto model)
        {
            var result = new PickUpAttachmentReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new PickUpAttachmentCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 标记邮件为已读状态，且如果有附件则领取附件。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<MakeReadAndPickUpReturnDto> MakeReadAndPickUp(MakeReadAndPickUpParamsDto model)
        {
            var result = new MakeReadAndPickUpReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            //获取附件
            var command = new PickUpAttachmentCommand { GameChar = gc, };
            command.MailIds.AddRange(model.MailIds);
            _SyncCommandManager.Handle(command);
            if (command.HasError)
            {
                result.FillErrorFrom(command);
                return result;
            }
            _Mapper.Map(command.Changes, result.Changes);
            //标记已读
            var commandMakeRead = new MakeMailReadCommand { GameChar = gc };
            commandMakeRead.MailIds.AddRange(model.MailIds);
            _SyncCommandManager.Handle(commandMakeRead);
            if (commandMakeRead.HasError)
            {
                result.FillErrorFrom(commandMakeRead);
                return result;
            }

            return result;
        }
    }

}
