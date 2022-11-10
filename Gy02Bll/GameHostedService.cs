using GuangYuan.GY001.TemplateDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics;

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
            var result = base.StartAsync(cancellationToken).ContinueWith(Post);
            return result;
        }

        /// <summary>
        /// 在基类<see cref="StartAsync(CancellationToken)"/>任务返回后运行的后续任务。
        /// </summary>
        void Post(Task task)
        {
            Test();

        }

        [Conditional("DEBUG")]
        private void Test()
        {
            var ss=_Services.GetService<AutoClearPool<List<int>>>();
        }
    }
}