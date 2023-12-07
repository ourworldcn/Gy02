using AutoMapper;
using GY02.Base;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;

namespace OW.Game.Manager
{
    /// <summary>
    /// TODO 标记强类型对象的模板Id。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TemplateIdAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tidString"><see cref="Guid"/>的字符串表述形式。</param>
        public TemplateIdAttribute(string tidString)
        {
            _TemplateId = Guid.Parse(tidString);

            // Implement code here

        }

        /// <summary>
        /// See the attribute guidelines at 
        /// http://go.microsoft.com/fwlink/?LinkId=85236
        /// </summary>
        readonly Guid _TemplateId;
        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId
        {
            get { return _TemplateId; }
        }

    }

    public class VirtualThingManagerOptions : IOptions<VirtualThingManagerOptions>
    {
        public VirtualThingManagerOptions Value => this;
    }

    /// <summary>
    /// 虚拟物管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class VirtualThingManager : GameManagerBase<VirtualThingManagerOptions, VirtualThingManager>
    {
        public VirtualThingManager(IOptions<VirtualThingManagerOptions> options, ILogger<VirtualThingManager> logger, GameTemplateManager templateManager, IMapper mapper) : base(options, logger)
        {
            _TemplateManager = templateManager;

            _Mapper = mapper;
            Initialize();
        }

        void Initialize()
        {

        }

        GameTemplateManager _TemplateManager;

        IMapper _Mapper;

        /// <summary>
        /// 对于缺失子项的虚拟对象，补足缺失的虚拟对象。
        /// </summary>
        /// <param name="root">修补此对象下 所有的缺失对象。</param>
        /// <returns>true修补了对象，false没有修补。</returns>
        public bool Normal(VirtualThing root)
        {
            var ignTids = new Guid[] { Guid.Parse("29b7e726-387f-409d-a6ac-ad8670a814f0"), Guid.Parse("14d0e372-909b-485f-b8cb-07c9231b10ff") };
            if (ignTids.Contains(root.ExtraGuid) || root.Children.Count > 0)
                return false;
            var result = false;
            var tt = _TemplateManager.GetFullViewFromId(root.ExtraGuid);    //根的子对象
            if (tt.TIdsOfCreate is null) return result;    //若没有指定子对象
            var tids = root.Children.Select(c => c.ExtraGuid).ToArray();    //已有子对象tid集合
            var list = tt.TIdsOfCreate.Where(c => !tids.Contains(c)).ToArray();   //需要补足的对象tid集合
            foreach (var item in list)  //补足子对象
            {
                var template = _TemplateManager.GetFullViewFromId(item);    //获取模板
                if (template is null) continue;
                var thing = Create(template);   //创建对象
                if (thing is null) continue;
                root.Children.Add(thing);
                result = true;
            }
            foreach (var item in root.Children) //修补子对象
            {
                result = Normal(item) || result;
            }
            return result;
        }

        /// <summary>
        /// 用指定的模板Id创建对象。
        /// </summary>
        /// <param name="tId"></param>
        /// <param name="count">创建多少个对象。</param>
        /// <returns>创建对象的数组，任何创建失败都会导致返回null，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public VirtualThing[] Create(Guid tId, int count)
        {
            var tt = _TemplateManager.GetFullViewFromId(tId);
            if (tt is null) return null;    //若无法找到指定的模板
            VirtualThing[] result = new VirtualThing[count];
            VirtualThing tmp;
            for (int i = 0; i < count; i++)
            {
                tmp = Create(tt);
                if (tmp is null)
                    return null;
                result[i] = tmp;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tv">创建的模板。</param>
        /// <param name="changes">记录详细变化数据的集合，省略或为null则忽略，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</param>
        /// <returns></returns>
        public VirtualThing Create(TemplateStringFullView tv, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            VirtualThing result = new VirtualThing { };
#if DEBUG 
            _TemplateManager.SetTemplate(result);
#endif //DEBUG
            var type = GameTemplateManager.GetTypeFromTemplate(tv);    //获取实例类型

            var view = result.GetJsonObject(type);
            _Mapper.Map(tv, view, tv.GetType(), view.GetType()); //复制一般性属性。
            if (tv.Fcps.Count > 0)   //TODO 临时修正
            {
                dynamic dyn = view;
                dyn.Fcps.Clear();
                foreach (var fcps in tv.Fcps)
                {
                    var fcp = (FastChangingProperty)fcps.Value.Clone();
                    var ov = fcp.CurrentValue;
                    fcp.GetCurrentValueWithUtc();
                    fcp.CurrentValue = ov;
                    dyn.Fcps.Add(fcps.Key, fcp);
                }
            }
            if (tv.TIdsOfCreate is not null)
                foreach (var item in tv.TIdsOfCreate) //创建所有子对象
                {
                    var subTT = _TemplateManager.Id2FullView[item];
                    var sub = Create(subTT);   //创建子对象
                    Add(sub, result, changes);
                }
            return result;
        }

        /// <summary>
        /// 从数据库中删除指定的虚拟对象。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="context"></param>
        /// <returns>true成功删除，false不在指定的上下文中。</returns>
        public bool Delete(VirtualThing thing, DbContext context)
        {
            bool result;
            if (thing.Parent is not null)
            {
                thing.Parent.Children.Remove(thing);
                thing.Parent = null;
                result = true;
            }
            else
                result = false;
            thing.ParentId = null;
            var efEntity = context.Entry(thing);
            if (efEntity.State != EntityState.Detached) //若不是游离对象
                _ = context.Remove(thing);
            return result;
        }

        /// <summary>
        /// 向指定容器添加一项，会自动将子项从原有父(如果有)中移除。
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <param name="changes"></param>
        public static void Add(VirtualThing child, VirtualThing parent, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (child.Parent is not null) Remove(child, child.Parent, changes);
            parent.Children.Add(child);
            child.Parent = parent;
            child.ParentId = parent.Id;
            changes?.CollectionAdd(child, parent);
        }

        /// <summary>
        /// 从指定父中移除子项。不会从数据库中删除子项。
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public static bool Remove(VirtualThing child, VirtualThing parent, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var result = parent.Children.Remove(child);
            if (result)
                changes?.CollectionRemove(child, parent);
            return result;
        }
    }

}
