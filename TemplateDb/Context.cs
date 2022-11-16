using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GuangYuan.GY001.TemplateDb
{

    public static class TemplateMigrateDbInitializer
    {
        public static void Initialize(GY02TemplateContext context)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }
    }

}
