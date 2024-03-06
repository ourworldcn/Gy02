using GY02;
using GY02.AutoMappper;
using GY02.Base;
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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using OW.GameDb;
using OW.SyncCommand;
using System.IO.Compression;

internal class Program
{
    private static void Main(string[] args)
    {
    lbStart:
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddMemoryCache();
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

        services.AddPooledDbContextFactory<GY02LogginContext>(options => options.UseLazyLoadingProxies().UseSqlServer(loggingDbConnectionString).EnableSensitiveDataLogging());

        services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);
        //services.AddDbContext<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
        services.AddDbContextFactory<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());

        VWorld.TemplateContextOptions = new DbContextOptionsBuilder<GY02TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options;
        VWorld.UserContextOptions = new DbContextOptionsBuilder<GY02UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options;

        #endregion 配置数据库
        if (TimeSpan.TryParse(builder.Configuration.GetSection("WorldClockOffset").Value, out var offerset))
            OwHelper._Offset = offerset;  //配置游戏世界的时间。
        //services.Replace(new ServiceDescriptor(typeof(ISystemClock), typeof(OwSystemClock), ServiceLifetime.Singleton));
        services.AddGameServices();
        services.AddHostedService<GameHostedService>();

        services.AddOptions().Configure<RawTemplateOptions>(builder.Configuration.GetSection("GameTemplates"))
            .Configure<UdpServerManagerOptions>(builder.Configuration.GetSection("UdpServerManagerOptions"))
            .Configure<LoginNameGeneratorOptions>(builder.Configuration.GetSection("LoginNameGeneratorOptions"));  //模板配置的选项模式

        services.AddAutoMapper(typeof(Gy02AutoMapperProfile).Assembly, typeof(GameCharDto).Assembly, typeof(GY02AutoMapperProfile).Assembly);
        services.AddPublisherT78();
        services.AddPublisherT127();
        services.AddPublisherT1228();
        var app = builder.Build();

        #endregion 追加服务到容器

        #region 配置HTTP管道

        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())

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


}
