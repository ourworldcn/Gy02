using AutoMapper;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Concurrent;
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
        static ConcurrentDictionary<Guid, Type> _TypeGuid2Type;
        public static ConcurrentDictionary<Guid, Type> TypeGuid2Type
        {
            get
            {
                if (_TypeGuid2Type is null)
                {
                    var coll = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes()).Where(c => !c.IsAbstract && c.IsAssignableTo(typeof(OwGameEntityBase)));
                    var tmp = new ConcurrentDictionary<Guid, Type>(coll.ToDictionary(c => c.GUID));
                    Interlocked.CompareExchange(ref _TypeGuid2Type, tmp, null);
                }
                return _TypeGuid2Type;
            }
        }

        /// <summary>
        /// 获取模板的生成类的类型。
        /// </summary>
        /// <param name="fullView"></param>
        /// <returns></returns>
        public static Type GetTypeFromTemplate(TemplateStringFullView fullView)
        {
            var result = TypeGuid2Type.GetValueOrDefault(fullView.TypeGuid);    //获取实例类型
            if (result is not null && fullView.SubTypeGuid is not null) //若是泛型类
            {
                var sub = TypeGuid2Type.GetValueOrDefault(fullView.SubTypeGuid.Value);
                result = result.MakeGenericType(sub);
            }
            return result;
        }

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
            var tt = tm.GetRawTemplateFromId(command.TemplateId);  //获取模板
            if (tt is null)
            {
                command.HasError = true;
                command.DebugMessage = $"找不到指定模板，Id={command.TemplateId}";
            }
            else
                command.Result = Create(tt);
        }

        VirtualThing Create(RawTemplate template)
        {
            var tm = _Service.GetRequiredService<TemplateManager>();
            var mapper = _Service.GetRequiredService<IMapper>();
            var tv = template.GetJsonObject<TemplateStringFullView>();
            VirtualThing result = new VirtualThing();
            var type = GetTypeFromTemplate(tv);    //获取实例类型

            var view = result.GetJsonObject(type);
            mapper.Map(tv, view, tv.GetType(), view.GetType()); //复制一般性属性。

            if (tv.TIdsOfCreate is not null)
                foreach (var item in tv.TIdsOfCreate) //创建所有子对象
                {
                    var tt = tm.GetRawTemplateFromId(item);
                    var sub = Create(tt);   //创建子对象
                    result.Children.Add(sub);
                    sub.Parent = result;
                    sub.ParentId = result.Id;
                }
            return result;
        }
    }
}
