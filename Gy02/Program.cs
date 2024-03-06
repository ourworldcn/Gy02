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
        #region ����ѹ��

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

        #endregion ����ѹ��

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;  //ֱ����������
                                                                        //options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;  //����ֻ�����ԡ�
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        
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

        services.AddPooledDbContextFactory<GY02LogginContext>(options => options.UseLazyLoadingProxies().UseSqlServer(loggingDbConnectionString).EnableSensitiveDataLogging());

        services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);
        //services.AddDbContext<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
        services.AddDbContextFactory<GY02UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging());

        VWorld.TemplateContextOptions = new DbContextOptionsBuilder<GY02TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options;
        VWorld.UserContextOptions = new DbContextOptionsBuilder<GY02UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options;

        #endregion �������ݿ�
        if (TimeSpan.TryParse(builder.Configuration.GetSection("WorldClockOffset").Value, out var offerset))
            OwHelper._Offset = offerset;  //������Ϸ�����ʱ�䡣
        //services.Replace(new ServiceDescriptor(typeof(ISystemClock), typeof(OwSystemClock), ServiceLifetime.Singleton));
        services.AddGameServices();
        services.AddHostedService<GameHostedService>();

        services.AddOptions().Configure<RawTemplateOptions>(builder.Configuration.GetSection("GameTemplates"))
            .Configure<UdpServerManagerOptions>(builder.Configuration.GetSection("UdpServerManagerOptions"))
            .Configure<LoginNameGeneratorOptions>(builder.Configuration.GetSection("LoginNameGeneratorOptions"));  //ģ�����õ�ѡ��ģʽ

        services.AddAutoMapper(typeof(Gy02AutoMapperProfile).Assembly, typeof(GameCharDto).Assembly, typeof(GY02AutoMapperProfile).Assembly);
        services.AddPublisherT78();
        services.AddPublisherT127();
        services.AddPublisherT1228();
        var app = builder.Build();

        #endregion ׷�ӷ�������

        #region ����HTTP�ܵ�

        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())

        IWebHostEnvironment env = app.Environment;
        app.UseResponseCompression();
        app.UseResponseCaching();
        app.UseDeveloperExceptionPage();

        app.UseStaticFiles();
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
    }
}

#region ׷�ӷ�������
#region ����ѹ��

#endregion ����ѹ��

#region ����Swagger

#endregion ����Swagger
#region �������ݿ�

#endregion �������ݿ�

#endregion ׷�ӷ�������
#region ����HTTP�ܵ�

#region �����м����������Swagger

#endregion �����м����������Swagger
#endregion ����HTTP�ܵ�

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
