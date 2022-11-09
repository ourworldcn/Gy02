using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.ComponentModel.DataAnnotations;

namespace GuangYuan.GY001.TemplateDb
{

    /// <summary>
    /// 游戏模板数据库上下文。
    /// </summary>
    public class GY02TemplateContext : GameTemplateContext
    {
        public GY02TemplateContext()
        {

        }

        public GY02TemplateContext(DbContextOptions<GY02TemplateContext> options) : base(options)
        {

        }

        /// <summary>
        /// 装备表。
        /// </summary>
        public DbSet<GameItemTemplate> ItemTemplates { get; set; }

        ///// <summary>
        ///// 蓝图表
        ///// </summary>
        //public DbSet<BlueprintTemplate> BlueprintTemplates { get; set; }

        ///// <summary>
        ///// 属性定义表。
        ///// </summary>
        //public DbSet<GamePropertyTemplate> GamePropertyTemplates { get; set; }

        ///// <summary>
        ///// 商城定义表。
        ///// </summary>
        //public DbSet<GameShoppingTemplate> ShoppingTemplates { get; set; }

        ///// <summary>
        ///// 任务定义。
        ///// </summary>
        //public DbSet<GameMissionTemplate> MissionTemplates { get; set; }

        ///// <summary>
        ///// 卡池定义表。
        ///// </summary>
        //public DbSet<GameCardPoolTemplate> CardPoolTemplates { get; set; }
    }

    public static class TemplateMigrateDbInitializer
    {
        public static void Initialize(GY02TemplateContext context)
        {
            context.Database.Migrate();
        }
    }

}
