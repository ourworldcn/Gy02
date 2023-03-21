using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class CreateVirtualThingCommand : SyncCommandBase
    {
        public CreateVirtualThingCommand()
        {
        }

        /// <summary>
        /// 创建虚拟对象的模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 返回时创建的对象。
        /// </summary>
        public VirtualThing Result { get; set; }
    }

    /// <summary>
    /// 创建虚拟对象的命令处理类。
    /// </summary>
    public class CreateVirtualThingHandler : SyncCommandHandlerBase<CreateVirtualThingCommand>
    {
        public CreateVirtualThingHandler(IServiceProvider service)
        {
            _Service = service;
        }

        /// <summary>
        /// 
        /// </summary>
        public IServiceProvider _Service { get; set; }

        /// <summary>
        /// 创建 <see cref="VirtualThing"/> 。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateVirtualThingCommand command)
        {
            //var tm = _Service.GetRequiredService<TemplateManager>();
            //var tt = tm.GetTemplateFromId(command.TemplateId);  //获取模板
            //if (tt is null)
            //{
            //    command.HasError = true;
            //    command.DebugMessage = $"找不到指定模板，Id={command.TemplateId}";
            //}
            //else
            //    command.Result = Create(tt.GetJsonObject<Gy02TemplateJO>());
        }

    }
}
