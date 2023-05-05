using Castle.Core.Configuration;
using GuangYuan.GY001.TemplateDb;
using Gy02;
using Gy02.AutoMappper;
using Gy02.Publisher;
using Gy02Bll;
using Gy02Bll.Base;
using Gy02Bll.Managers;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

lbStart:
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.Configuration.AddJsonFile("GameTemplates.json", false, true);    //加入模板信息配置文件
//builder.Services.AddW3CLogging(logging =>
//{
//    // Log all W3C fields
//    logging.LoggingFields = W3CLoggingFields.All;

//    logging.FileSizeLimit = 5 * 1024 * 1024;
//    logging.RetainedFileCountLimit = 2;
//    logging.FileName = "MyLogFile";
//    logging.LogDirectory = @"C:\logs";
//    logging.FlushInterval = TimeSpan.FromSeconds(2);
//});

#region 追加服务到容器

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;  //直接用属性名
    //options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;  //忽略只读属性。
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

services.AddResponseCompression(c => c.EnableForHttps = true);

#region 配置Swagger
//注册Swagger生成器，定义一个 Swagger 文档
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = $"光元02",
        Description = "接口文档v2.0.0",
        Contact = new OpenApiContact() { }
    });
    // 为 Swagger 设置xml文档注释路径
    var fileNames = Directory.GetFiles(AppContext.BaseDirectory, "*ApiDoc.xml");
    foreach (var item in fileNames) //加入多个xml描述文件
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, item);
        c.IncludeXmlComments(xmlPath, true);
    }
    c.OrderActionsBy(c => c.RelativePath);
});
#endregion 配置Swagger

#region 配置数据库
var loggingDbConnectionString = builder.Configuration.GetConnectionString("LoggingDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var userDbConnectionString = builder.Configuration.GetConnectionString("UserDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var templateDbConnectionString = builder.Configuration.GetConnectionString("TemplateDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);

services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);
//services.AddDbContext<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
services.AddDbContextFactory<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());

VWorld.TemplateContextOptions = new DbContextOptionsBuilder<GY02TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options;
VWorld.UserContextOptions = new DbContextOptionsBuilder<GY02UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options;

#endregion 配置数据库

services.AddGameServices();
services.AddHostedService<GameHostedService>();

services.AddOptions().Configure<RawTemplateOptions>(builder.Configuration.GetSection("GameTemplates"))
    .Configure<UdpServerManagerOptions>(builder.Configuration.GetSection("UdpServerManagerOptions"));  //模板配置的选项模式

services.AddAutoMapper(typeof(Gy02AutoMapperProfile).Assembly, typeof(GameCharDto).Assembly, typeof(Gy02BllAutoMapperProfile).Assembly);
services.AddPublisherT78();

var app = builder.Build();

#endregion 追加服务到容器

#region 配置HTTP管道

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())

IWebHostEnvironment env = app.Environment;

//app.UseAuthorization();

app.MapControllers();

#region 启用中间件服务生成Swagger
app.UseSwagger();
//app.UseSwaggerUI();
//启用中间件服务生成SwaggerUI，指定Swagger JSON终结点
app.UseSwaggerUI(c =>
{
    //c.SwaggerEndpoint("/swagger/v2/swagger.json", env.EnvironmentName + $" V2");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{env.EnvironmentName} V1");
    c.RoutePrefix = string.Empty;//设置根节点访问
});
#endregion 启用中间件服务生成Swagger


#endregion 配置HTTP管道

app.Run();

if (Global.Program.ReqireReboot) //若需要重启
{
    (app as IDisposable)?.Dispose();
    app = null;
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
    GC.WaitForFullGCComplete();
    Global.Program.ReqireReboot = false;
    goto lbStart;
}

namespace Global
{
    /// <summary>
    /// 
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// 是否请求重新启动。
        /// </summary>
        public static volatile bool ReqireReboot = false;

    }
}
