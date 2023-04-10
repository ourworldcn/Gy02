using OW.Game.Manager;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    /// <summary>
    /// 创建多个虚拟对象。
    /// </summary>
    public class CreateVirtualThingsCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 要创建的虚拟对象的模板Id集合。
        /// 注意这里不考虑数量问题。如果有一个模板Id就会创建一个对象，多个相同模板Id会创建多个相同模板的对象。
        /// </summary>
        public List<Guid> Items { get; set; } = new List<Guid> { };

        /// <summary>
        /// 返回创建的对象集合。
        /// 如果一个创建有问题，则不会返回任何结果。
        /// </summary>
        public List<VirtualThing> Result { get; set; } = new List<VirtualThing>();
    }

    public class CreateVirtualThingsHandler : SyncCommandHandlerBase<CreateVirtualThingsCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="virtualThingManager"></param>
        public CreateVirtualThingsHandler(VirtualThingManager virtualThingManager)
        {
            _VirtualThingManager = virtualThingManager;
        }

        VirtualThingManager _VirtualThingManager;

        /// <summary>
        /// 处理函数。
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CreateVirtualThingsCommand command)
        {
        }
    }
}
