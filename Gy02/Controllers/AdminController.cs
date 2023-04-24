using AutoMapper.Configuration.Annotations;
using Gy02Bll.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using OW.Game.Manager;
using OW.Game.Managers;
using System.IO.Compression;
using System.Text.Json;

namespace Gy02.Controllers
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
        /// 上传游戏模板数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token">令牌。</param>
        /// <param name="applicationLifetime"></param>
        /// <returns></returns>
        [HttpPost,]
        public ActionResult ImportTemplates(IFormFile file, string token, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            using var stream = file.OpenReadStream();
            try
            {
                var list = JsonSerializer.Deserialize<TemplateDatas>(stream);
                var dic = TemplateManager.GetTemplateFullviews(list?.GameTemplates);

                //stream.Seek(0, SeekOrigin.Begin);
                //var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameTemplates.json");
                //using var writer = System.IO.File.OpenWrite(path);
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

            return Ok("成功上传，服务器重启，5秒后重新连接。");
        }

    }
}
