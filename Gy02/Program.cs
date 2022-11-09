using GuangYuan.GY001.TemplateDb;
using Gy02Bll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

#region 配置Swagger
//注册Swagger生成器，定义一个 Swagger 文档
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = $"光元02",
        Description = "接口文档v2.0.0"
    });
    // 为 Swagger 设置xml文档注释路径
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
#endregion 配置Swagger

#region 配置数据库
var loggingDbConnectionString = builder.Configuration.GetConnectionString("LoggingDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var userDbConnectionString = builder.Configuration.GetConnectionString("UserDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var templateDbConnectionString = builder.Configuration.GetConnectionString("TemplateDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);

services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);

#endregion 配置数据库

services.AddHostedService<GameHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())

IWebHostEnvironment env = app.Environment;

#region 启用中间件服务生成Swagger
app.UseSwagger();
app.UseSwaggerUI();
//启用中间件服务生成SwaggerUI，指定Swagger JSON终结点
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", env.EnvironmentName + $" V2");
//    c.RoutePrefix = string.Empty;//设置根节点访问
//});
#endregion 启用中间件服务生成Swagger


//app.UseAuthorization();

app.MapControllers();

app.Run();
