using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game;
using OW.Game.Managers;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class CreateVirtualThingCommand : GameCommandBase
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
    public class CreateVirtualThingHandler : GameCommandHandlerBase<CreateVirtualThingCommand>
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
            var tm = _Service.GetRequiredService<TemplateManager>();
            var tt = tm.GetTemplateFromId(command.TemplateId);  //获取模板
            if (tt is null)
            {
                command.HasError = true;
                command.DebugMessage = $"找不到指定模板，Id={command.TemplateId}";
            }
            else
                command.Result = Create(tt.GetJsonObject<Gy02TemplateJO>());
        }

        /// <summary>
        /// 用指定模板创建一个<see cref="VirtualThing"/>。
        /// </summary>
        /// <param name="template">创建对象使用的模板。</param>
        /// <returns></returns>
        public VirtualThing Create(Gy02TemplateJO template)
        {
            var result = new VirtualThing() { ExtraGuid = template.Id, };
            //var view = template.GetJsonObject<Gy02TemplateJO>();
            //复制必要属性
            var dic = AutoClearPool<Dictionary<string, object>>.Shared.Get();
            using var dwReturn = DisposeHelper.Create(AutoClearPool<Dictionary<string, object>>.Shared.Return, dic);    //确保回收

            OwHelper.Copy(template.ExtraProperties, dic);
            foreach (var item in template.UpgradeInfo.DecimalProperties)
            {
                dic[item.Key] = item.Value?[0] ?? decimal.Zero;
            }
            result.JsonObjectString = JsonSerializer.Serialize(dic);

            //初始化子对象
            var gtm = _Service.GetRequiredService<TemplateManager>();
            foreach (var item in template.CreateInfo.ChildrenTIds)
            {
                var tt = gtm.GetTemplateFromId(item);
                var thing = Create(tt.GetJsonObject<Gy02TemplateJO>());

                result.Children.Add(thing);
                thing.ParentId = result.Id;
                thing.Parent = result;
            }
            return result;
        }

    }
}
