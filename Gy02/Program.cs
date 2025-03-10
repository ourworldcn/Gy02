using Global;
using GY02;
using GY02.AutoMappper;
using GY02.Base;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.TemplateDb;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using OW.GameDb;
using OW.SyncCommand;
using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
    lbStart:
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddMemoryCache();
        //加入模板信息配置文件
        builder.Configuration.AddJsonFile("GameTemplates.json", false, true);
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
        //需要文件日志则用Serilog.Extensions.Logging.File
        builder.Logging.AddEventLog(eventLogSettings =>
        {
            eventLogSettings.SourceName = "OwLogs";
        });
        //var fileListener = new TextWriterTraceListener("OwLogs.txt");
        //builder.Logging.AddTraceSource(new SourceSwitch("Debug"),fileListener);
        //Trace.Listeners.Add(fileListener);

        #region 追加服务到容器

        // Add services to the container.
        #region 配置压缩

        services.AddResponseCompression(c =>
        {
            c.EnableForHttps = true;
            c.Providers.Add<BrotliCompressionProvider>();  //ICompressionProvider
            c.Providers.Add<GzipCompressionProvider>(); //ICompressionProvider
            c.Providers.Add<OwDeflateCompressionProvider>();
        })
        .Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; })
        .Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });
        //.Configure<OwDeflateCompressionProvider>(options=> options.le);

        #endregion 配置压缩

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;  //直接用属性名
                                                                        //options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;  //忽略只读属性。
        });

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
                Description = $"接口文档v2.0.0({builder.Environment.EnvironmentName})",
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

        services.AddPooledDbContextFactory<GY02LogginContext>(options => options.UseLazyLoadingProxies().UseSqlServer(loggingDbConnectionString).EnableSensitiveDataLogging());

        services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);
        //services.AddDbContext<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
        services.AddDbContextFactory<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());

        VWorld.TemplateContextOptions = new DbContextOptionsBuilder<GY02TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options;
        VWorld.UserContextOptions = new DbContextOptionsBuilder<GY02UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options;

        #endregion 配置数据库

        if (TimeSpan.TryParse(builder.Configuration.GetSection("WorldClockOffset").Value, out var offerset))
            OwHelper.Offset = offerset;  //配置游戏世界的时间。
        //services.Replace(new ServiceDescriptor(typeof(ISystemClock), typeof(OwSystemClock), ServiceLifetime.Singleton));
        services.AddGameServices();
        services.AddHostedService<GameHostedService>();

        services.AddOptions().Configure<RawTemplateOptions>(builder.Configuration.GetSection("GameTemplates"))
            .Configure<UdpServerManagerOptions>(builder.Configuration.GetSection("UdpServerManagerOptions"))
            .Configure<OwRdmServerOptions>(builder.Configuration.GetSection("OwRdmServerOptions"))
            .Configure<LoginNameGeneratorOptions>(builder.Configuration.GetSection("LoginNameGeneratorOptions"))  //模板配置的选项模式
            .Configure<ButieOptions>(builder.Configuration.GetSection("ButieOptions")); //补钻石

        services.AddAutoMapper(typeof(Gy02AutoMapperProfile).Assembly, typeof(GameCharDto).Assembly, typeof(GY02AutoMapperProfile).Assembly);
        services.AddPublisherT78();
        services.AddPublisherT127();
        services.AddPublisherT1228();
        services.AddPublisherT0314();

        services.AddHttpClient("T1021/NA",c=>c.DefaultRequestHeaders.Add("contentType", "application/x-www-form-urlencoded"));

        services.AddScoped(c => new SimpleGameContext());
        services.AddSingleton<OwRdmServer>();

        var app = builder.Build();

        //app.Use(async (context, next) =>
        //{
        //    context.Request.EnableBuffering();
        //    var gcontext = context.RequestServices.GetRequiredService<SimpleGameContext>();
        //    try
        //    {
        //        var tokenModel = (await JsonSerializer.DeserializeAsync<TokenDtoBase>(context.Request.Body))!;
        //        if (tokenModel.Token != Guid.Empty)
        //        {
        //            gcontext.Token = tokenModel.Token;
        //            var _GameAccountStore = context.RequestServices.GetRequiredService<GameAccountStoreManager>();
        //            if (_GameAccountStore.Token2Key.TryGetValue(tokenModel.Token, out var key))
        //            {
        //                if (_GameAccountStore.Key2User.TryGetValue(key, out var user))
        //                    gcontext.GameChar = user.CurrentChar;
        //            }

        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    context.Request.Body.Position = 0;
        //    await next.Invoke();
        //});
        #endregion 追加服务到容器

        #region 配置HTTP管道

        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())

        app.UseMiddleware<GY02Middleware>();

        IWebHostEnvironment env = app.Environment;
        app.UseResponseCompression();
        app.UseResponseCaching();
        app.UseDeveloperExceptionPage();

        app.UseStaticFiles();
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
            Thread.Sleep(1000);
            Global.Program.ReqireReboot = false;
            goto lbStart;
        }
    }
}

#region 追加服务到容器
#region 配置压缩

#endregion 配置压缩

#region 配置Swagger

#endregion 配置Swagger
#region 配置数据库

#endregion 配置数据库

#endregion 追加服务到容器
#region 配置HTTP管道

#region 启用中间件服务生成Swagger

#endregion 启用中间件服务生成Swagger
#endregion 配置HTTP管道

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

    /// <summary>
    /// 项目自用中间件。
    /// </summary>
    public class GY02Middleware
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GY02Middleware(RequestDelegate next)
        {
            _Next = next;

            Initialize();
        }

        private void Initialize()
        {
        }

        RequestDelegate _Next;
        //SimpleGameContext _GameContext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="gameContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, SimpleGameContext gameContext)
        {
            try
            {
                await _Next(context);
            }
            finally
            {

            }
        }
    }


}
