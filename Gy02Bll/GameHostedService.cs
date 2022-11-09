using GuangYuan.GY001.TemplateDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gy02Bll
{
    public class GameHostedService : BackgroundService
    {


        public GameHostedService(IServiceProvider services)
        {
            _Services = services;
        }

        public IServiceProvider _Services { get; set; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var tContext = _Services.GetRequiredService<GY02TemplateContext>();
            TemplateMigrateDbInitializer.Initialize(tContext);
            //logger.LogTrace($"模板数据库已正常升级。");
            var result = base.StartAsync(cancellationToken);
            return result;
        }
    }
}