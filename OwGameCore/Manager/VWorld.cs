using GuangYuan.GY001.TemplateDb;
using GY02.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Manager
{
    public class VWorld
    {
        /// <summary>
        /// 
        /// </summary>
        public static DbContextOptions<GY02TemplateContext> TemplateContextOptions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static DbContextOptions<GY02UserContext> UserContextOptions { get; set; }

        /// <summary>
        /// 新建一个用户数据库的上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GY02UserContext CreateNewUserDbContext() =>
            new GY02UserContext(UserContextOptions);

        /// <summary>
        /// 记录应用的根服务容器。
        /// </summary>
        public static IServiceProvider Service { get; set; }

        /// <summary>
        /// 默认的Json序列化配置对象。
        /// </summary>
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, };

    }
}
