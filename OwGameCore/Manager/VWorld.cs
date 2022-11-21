using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
        public static DbContextOptions<GameUserContext> UserContextOptions { get; set; }

        /// <summary>
        /// 新建一个用户数据库的上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GameUserContext CreateNewUserDbContext() =>
            new GameUserContext(UserContextOptions);
    }
}
