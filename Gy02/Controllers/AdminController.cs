using AutoMapper.Configuration.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Manager;
using System.IO.Compression;

namespace Gy02.Controllers
{
    /// <summary>
    /// 管理员工具的控制器。
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : GameControllerBase
    {
        /// <summary>
        /// 上传用户数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token">令牌。</param>
        /// <returns></returns>
        [HttpPost,]
        public ActionResult ImportUsers(IFormFile file, string token)
        {
            using var stream = file.OpenReadStream();
            //using var datas = new ImportUsersDatas(World, token) { Store = stream };

            //using var cStream = new BrotliStream(stream, CompressionMode.Decompress);
            //datas.Store = cStream;

            //World.AdminManager.ImportUsers(datas);
            //if (datas.HasError)
            //    return BadRequest();
            //else
            return Ok();
        }

    }
}
