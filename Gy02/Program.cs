using Castle.Core.Configuration;
using GuangYuan.GY001.TemplateDb;
using Gy02;
using Gy02.AutoMappper;
using Gy02Bll;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Store;
using System.Diagnostics;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

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
services.AddDbContext<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);

VWorld.TemplateContextOptions = new DbContextOptionsBuilder<GY02TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options;
VWorld.UserContextOptions = new DbContextOptionsBuilder<GY02UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options;

#endregion 配置数据库

services.AddGameServices();
services.AddHostedService<GameHostedService>();

services.AddOptions();

services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);


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
