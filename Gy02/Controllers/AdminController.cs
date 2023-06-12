using AutoMapper.Configuration.Annotations;
using GY02;
using GY02.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using OW.Game.Manager;
using OW.Game.Managers;
using System.IO.Compression;
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
        /// <param name="applicationLifetime"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        [HttpPost,]
        public ActionResult VerifyTemplates(IFormFile file, string token, [FromServices] IHostApplicationLifetime applicationLifetime, [FromServices] IHostEnvironment environment)
        {
            using var stream = file.OpenReadStream();
            try
            {
                var list = JsonSerializer.Deserialize<TemplateDatas>(stream);
                var dic = TemplateManager.GetTemplateFullviews(list?.GameTemplates);

                stream.Seek(0, SeekOrigin.Begin);
                //var path = Path.Combine(environment.ContentRootPath, "GameTemplates.json");
                //System.IO.File.Copy()
                //using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                //stream.CopyTo(writer);

                //Global.Program.ReqireReboot = true;
                //applicationLifetime.StopApplication();
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
        /// 上传模板数据。随后应重启数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token"></param>
        /// <param name="applicationLifetime"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        [HttpPost,]
        public ActionResult UploadTemplates(IFormFile file, string token, [FromServices] IHostApplicationLifetime applicationLifetime, [FromServices] IHostEnvironment environment)
        {
            using var stream = file.OpenReadStream();
            try
            {
                var list = JsonSerializer.Deserialize<TemplateDatas>(stream);
                var dic = TemplateManager.GetTemplateFullviews(list?.GameTemplates);

                stream.Seek(0, SeekOrigin.Begin);
                var str= "~/GameTemplates.json";
                
                var path = Path.Combine(environment.ContentRootPath, "GameTemplates.json");
                
                using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                stream.CopyTo(writer);

                //Global.Program.ReqireReboot = true;
                //applicationLifetime.StopApplication();
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
    }
}
