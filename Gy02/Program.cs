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

builder.Configuration.AddJsonFile("GameTemplates.json", false, true);    //����ģ����Ϣ�����ļ�
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

#region ׷�ӷ�������

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;  //ֱ����������
    //options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;  //����ֻ�����ԡ�
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

services.AddResponseCompression(c => c.EnableForHttps = true);

#region ����Swagger
//ע��Swagger������������һ�� Swagger �ĵ�
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = $"��Ԫ02",
        Description = "�ӿ��ĵ�v2.0.0",
        Contact = new OpenApiContact() { }
    });
    // Ϊ Swagger ����xml�ĵ�ע��·��
    var fileNames = Directory.GetFiles(AppContext.BaseDirectory, "*ApiDoc.xml");
    foreach (var item in fileNames) //������xml�����ļ�
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, item);
        c.IncludeXmlComments(xmlPath, true);
    }
    c.OrderActionsBy(c => c.RelativePath);
});
#endregion ����Swagger

#region �������ݿ�
var loggingDbConnectionString = builder.Configuration.GetConnectionString("LoggingDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var userDbConnectionString = builder.Configuration.GetConnectionString("UserDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var templateDbConnectionString = builder.Configuration.GetConnectionString("TemplateDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);

services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);
//services.AddDbContext<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
services.AddDbContextFactory<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());

VWorld.TemplateContextOptions = new DbContextOptionsBuilder<GY02TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options;
VWorld.UserContextOptions = new DbContextOptionsBuilder<GY02UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options;

#endregion �������ݿ�

services.AddGameServices();
services.AddHostedService<GameHostedService>();

services.AddOptions().Configure<RawTemplateOptions>(builder.Configuration.GetSection("GameTemplates"))
    .Configure<UdpServerManagerOptions>(builder.Configuration.GetSection("UdpServerManagerOptions"));  //ģ�����õ�ѡ��ģʽ

services.AddAutoMapper(typeof(Gy02AutoMapperProfile).Assembly, typeof(GameCharDto).Assembly, typeof(Gy02BllAutoMapperProfile).Assembly);
services.AddPublisherT78();

var app = builder.Build();

#endregion ׷�ӷ�������

#region ����HTTP�ܵ�

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())

IWebHostEnvironment env = app.Environment;

//app.UseAuthorization();

app.MapControllers();

#region �����м����������Swagger
app.UseSwagger();
//app.UseSwaggerUI();
//�����м����������SwaggerUI��ָ��Swagger JSON�ս��
app.UseSwaggerUI(c =>
{
    //c.SwaggerEndpoint("/swagger/v2/swagger.json", env.EnvironmentName + $" V2");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{env.EnvironmentName} V1");
    c.RoutePrefix = string.Empty;//���ø��ڵ����
});
#endregion �����м����������Swagger


#endregion ����HTTP�ܵ�

app.Run();

if (Global.Program.ReqireReboot) //����Ҫ����
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
        /// �Ƿ���������������
        /// </summary>
        public static volatile bool ReqireReboot = false;

    }
}
