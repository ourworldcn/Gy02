using AutoMapper;
using AutoMapper.Configuration.Annotations;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using OW.Game.Store.Base;
using OW.GameDb;
using OW.SyncCommand;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Web;

namespace GY02.Controllers
{
    /// <summary>
    /// 管理员工具的控制器。
    /// </summary>
    public class AdminController : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AdminController(GameAccountStoreManager gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager, GY02UserContext dbContext, GameRedeemCodeManager redeemCodeManager)
        {
            _AccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _DbContext = dbContext;
            _RedeemCodeManager = redeemCodeManager;
        }

        GameAccountStoreManager _AccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;
        GY02UserContext _DbContext;
        GameRedeemCodeManager _RedeemCodeManager;

        /// <summary>
        /// 封装模板数据配置文件的类。
        /// </summary>
        class TemplateDatas
        {
            public List<RawTemplate> GameTemplates { get; set; } = new List<RawTemplate>();
        }

        /// <summary>
        /// 校验游戏模板数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token">令牌。</param>
        /// 
        /// 
        /// <returns></returns>
        [HttpPost,]
        public ActionResult VerifyTemplates(IFormFile file, string token)
        {
            using var stream = file.OpenReadStream();
            try
            {
                using StreamReader sr = new StreamReader(stream);
                var str = sr.ReadToEnd();
                var list = JsonSerializer.Deserialize<TemplateDatas>(str);
                var dic = GameTemplateManager.GetTemplateFullviews(list?.GameTemplates);

                var coll = new List<ValidationResult>();
                dic.Values.SafeForEach(c =>
                {
                    Validator.TryValidateObject(c, new ValidationContext(c), coll, true);
                });
                if (coll.Count > 0)
                {
                    return BadRequest(coll);
                }
            }
            catch (AggregateException agg)
            {
                return BadRequest(agg.Message);
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }

            return Ok("格式正确。");
        }

        /// <summary>
        /// 上传模板数据。如果成功随后应重启数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token"></param>
        /// <param name="applicationLifetime"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        [HttpPost,]
        public ActionResult UploadTemplates(IFormFile file, string token, [FromServices] IHostApplicationLifetime applicationLifetime,
            [FromServices] IWebHostEnvironment environment)
        {
            if (!Guid.TryParse(token, out var tokenGuid) || tokenGuid != new Guid("{F871361D-A803-4F7E-B222-13216A89E9FA}"))
            {
                return Unauthorized("票据无效。");
            }

            using var stream = file.OpenReadStream();
            try
            {
                var list = JsonSerializer.Deserialize<TemplateDatas>(stream);
                var dic = GameTemplateManager.GetTemplateFullviews(list?.GameTemplates);

                stream.Seek(0, SeekOrigin.Begin);

                var path = Path.Combine(environment.ContentRootPath, "GameTemplates.json");
                using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                stream.CopyTo(writer);

                Global.Program.ReqireReboot = true;
                applicationLifetime.StopApplication();
            }
            catch (AggregateException agg)
            {
                return BadRequest(agg.Message);
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }

            return Ok("格式正确，并已上传。请重新启动服务器。");
        }

        /// <summary>
        ///  用一组登录名获取当前角色Id的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        [HttpPost,]
        public ActionResult<GetCharIdByLoginNameReturnDto> GetCharIdByLoginName(GetCharIdByLoginNameParamsDto model, [FromServices] GY02UserContext db)
        {
            var coll = from user in db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.UserTId && model.LoginNames.Contains(c.ExtraString))
                       join gc in db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.CharTId)
                       on user.Id equals gc.ParentId into gj
                       from dc in gj.DefaultIfEmpty()
                       select new { LoginName = user.ExtraString, CharId = dc.Id };
            var list = coll.ToArray();
            var result = new GetCharIdByLoginNameReturnDto { };
            result.LoginNames.AddRange(coll.Select(c => c.LoginName));
            result.CharIds.AddRange(coll.Select(c => c.CharId));
            return result;
        }

        /// <summary>
        /// 获取留存数据。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dbUser"></param>
        /// <param name="dbLogger"></param>
        /// <param name="entityManager"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetLiucunReturnDto> GetLiucun(GetLiucunParamsDto model, [FromServices] GY02UserContext dbUser, [FromServices] GY02LogginContext dbLogger, [FromServices] GameEntityManager entityManager)
        {
            if (model.Uid != "gy001" || model.Pwd != "210115")
                return Unauthorized();
            var result = new GetLiucunReturnDto();
            //取注册人数
            var regColl = from user in dbUser.VirtualThings
                          where user.ExtraGuid == ProjectContent.UserTId
                          select user;
            var ary = regColl.AsEnumerable().Select(c => c.GetJsonObject<GameUser>()).Where(c => c.CreateUtc >= model.StartReg && c.CreateUtc < model.EndReg).Select(c => c.Id).Distinct().ToArray();
            result.RegCount = ary.Length;

            var loginColl = from ar in dbLogger.ActionRecords
                            where ar.ActionId == "Loginged" && ar.WorldDateTime >= model.StartReg && ar.WorldDateTime < model.EndReg && ary.Contains(ar.ExtraGuid!.Value)
                            group ar.ExtraGuid by ar.ExtraGuid into g
                            select g.Key;

            result.LoginCount = loginColl.Count();
            if (result.RegCount > 0)
                result.Liucun = (decimal)result.LoginCount / result.RegCount;
            return result;
        }

        /// <summary>
        /// 修改服务器全局配置字典功能。仅超管和管理员可以成功执行。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ModifyServerDictionaryReturnDto> ModifyServerDictionary(ModifyServerDictionaryParamsDto model)
        {
            var result = new ModifyServerDictionaryReturnDto { };
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new ModifyServerDictionaryCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 获取服务器字典功能。任何登录用户都可以使用此功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetServerDictionaryReturnDto> GetServerDictionary(GetServerDictionaryParamsDto model)
        {
            var result = new GetServerDictionaryReturnDto { };
            //using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            //if (dw.IsEmpty)
            //{
            //    if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
            //    result.FillErrorFromWorld();
            //    return result;
            //}

            var command = new GetServerDictionaryCommand { /*GameChar = gc,*/ };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;

        }

        /// <summary>
        /// 生成兑换码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="500">指定的通用码重复。</response>  
        [HttpPost]
        public ActionResult<GenerateRedeemCodeReturnDto> GenerateRedeemCode(GenerateRedeemCodeParamsDto model)
        {
            var result = new GenerateRedeemCodeReturnDto();
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            if (gc.GetThing().Parent.GetJsonObject<GameUser>().LoginName != ProjectContent.AdminLoginName)   //若非超管账号
            {
                result.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                result.DebugMessage = "需要超管权限执行此操作";
                result.HasError = true;
                return result;
            }
            if (model.CodeType == 2)    //若是一次性码
            {
                var redeems = _RedeemCodeManager.Generat(model.Count, model.CodeType, _DbContext);
                var catalog = new GameRedeemCodeCatalog
                {
                    DisplayName = "",
                    CodeType = model.CodeType,
                    ShoppingTId = model.ShoppingItemTId,
                };
                _DbContext.Add(catalog);
                _DbContext.AddRange(redeems.Select(c => new GameRedeemCode
                {
                    Code = c,
                    CatalogId = catalog.Id,
                }));
                _DbContext.SaveChanges();
                result.Codes.AddRange(redeems);
            }
            else if (model.CodeType == 1)   //若是通用码
            {
                if (string.IsNullOrEmpty(model.Code))
                {
                    result.HasError = true;
                    result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    result.DebugMessage = $"生成通用码必须明确指定。";
                    return result;
                }
                var catalog = new GameRedeemCodeCatalog
                {
                    DisplayName = "",
                    CodeType = model.CodeType,
                    ShoppingTId = model.ShoppingItemTId,
                };
                _DbContext.Add(catalog);
                _DbContext.Add(new GameRedeemCode
                {
                    Code = model.Code,
                    CatalogId = catalog.Id,
                });
                _DbContext.SaveChanges();
                result.Codes.AddRange(new string[] { model.Code });
            }
            return result;
        }

        /// <summary>
        /// 修改系统时间。仅能开发调试版使用。需要超管权限执行此操作。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ModifyWorldDateTimeReturnDto> ModifyWorldDateTime(ModifyWorldDateTimeParamsDto model, [FromServices] IHostEnvironment environment)
        {
            var result = new ModifyWorldDateTimeReturnDto();
            if (environment.EnvironmentName != Environments.Production && environment.EnvironmentName != Environments.Development)
            {
                result.ErrorCode = ErrorCodes.ERROR_CALL_NOT_IMPLEMENTED;
                result.DebugMessage = "仅能开发调试版才能使用";
                return result;
            }
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            if (gc.GetThing().Parent.GetJsonObject<GameUser>().LoginName != "0A630B86-0C8F-4CDA-B9BB-A13E35295D71")   //若非超管账号
            {
                result.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                result.DebugMessage = "需要超管权限执行此操作。";
                return result;
            }
            OwHelper._Offset = TimeSpan.FromSeconds(model.Offset);
            return result;
        }
    }

}
