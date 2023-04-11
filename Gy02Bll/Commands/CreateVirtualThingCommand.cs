using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Base;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Entity;
using OW.Game.Manager;
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

        public CreateVirtualThingHandler(IServiceProvider service, TemplateManager templateManager, IMapper mapper, VirtualThingManager virtualThingManager)
        {
            _Service = service;
            _TemplateManager = templateManager;
            _Mapper = mapper;
            _VirtualThingManager = virtualThingManager;
        }

        /// <summary>
        /// 
        /// </summary>
        public IServiceProvider _Service { get; set; }

        TemplateManager _TemplateManager;
        VirtualThingManager _VirtualThingManager;
        IMapper _Mapper;

        /// <summary>
        /// 创建 <see cref="VirtualThing"/> 。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateVirtualThingCommand command)
        {
            var tm = _Service.GetRequiredService<TemplateManager>();
            var tt = tm.GetFullViewFromId(command.TemplateId);  //获取模板
            if (tt is null)
            {
                command.FillErrorFromWorld();
            }
            else
                command.Result = _VirtualThingManager.Create(tt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="count">对于可堆叠物品则创建单个对象，对于不可堆叠物品则创建指定数量的对象。</param>
        /// <param name="templateManager"></param>
        /// <param name="commandManager"></param>
        /// <returns>生成的虚拟物集合，如果有错则返回null.</returns>
        public static List<VirtualThing> CreateThing(Guid tid, decimal count, TemplateManager templateManager, SyncCommandManager commandManager)
        {
            var tt = templateManager.GetFullViewFromId(tid);
            if (tt is null) return null;
            var result = new List<VirtualThing>();
            if (tt.Stk == 1)   //若不可堆叠
            {
                for (int i = 0; i < count; i++)
                {
                    var command = new CreateVirtualThingCommand { TemplateId = tid };
                    commandManager.Handle(command);
                    if (command.HasError)
                    {
                        OwHelper.SetLastError(command.ErrorCode);
                        OwHelper.SetLastErrorMessage(command.DebugMessage);
                        return null;
                    }
                    if (templateManager.GetEntityBase(command.Result, out _) is GameEntity ge)
                    {
                        ge.Count = 1;
                        result.Add(command.Result);
                    }
                    else
                    {
                        OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                        return null;
                    }
                }
            }
            else //若可以堆叠
            {
                var command = new CreateVirtualThingCommand { TemplateId = tid };
                commandManager.Handle(command);
                if (command.HasError)
                {
                    OwHelper.SetLastError(command.ErrorCode);
                    OwHelper.SetLastErrorMessage(command.DebugMessage);
                    return null;
                }

                if (templateManager.GetEntityBase(command.Result, out _) is GameEntity ge)
                {
                    ge.Count = count;
                    result.Add(command.Result);
                }
                else
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    return null;
                }
            }
            return result;
        }
    }
}
