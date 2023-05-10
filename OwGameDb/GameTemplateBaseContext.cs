using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace OW.TemplateDb.Entity
{
    /// <summary>
    /// 游戏数据模板存储上下文。
    /// </summary>
    public class GameTemplateBaseContext : DbContext
    {
        public GameTemplateBaseContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected GameTemplateBaseContext()
        {
        }

    }

}
