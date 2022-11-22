using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace GuangYuan.GY001.TemplateDb.Entity
{
    /// <summary>
    /// 游戏模板存储上下文。
    /// </summary>
    public class GameTemplateContext : DbContext
    {
        public GameTemplateContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected GameTemplateContext()
        {
        }

    }

}
