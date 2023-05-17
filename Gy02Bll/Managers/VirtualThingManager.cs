using AutoMapper;
using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Caching;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
        public VirtualThingManager(IOptions<VirtualThingManagerOptions> options, ILogger<VirtualThingManager> logger, TemplateManager templateManager, IMapper mapper) : base(options, logger)
        {
            _TemplateManager = templateManager;

            Initialize();
            _Mapper = mapper;
        }

        void Initialize()
        {

        }

        TemplateManager _TemplateManager;

        IMapper _Mapper;

        #region 基础功能

        /// <summary>
        /// 获取虚拟对象树中存储的数据库上下文对象。
        /// </summary>
        /// <param name="thing">树中节点，数据库上下文存储在根节点的<see cref="VirtualThingBase.RuntimeProperties"/>中。</param>
        /// <returns>没有或出错可能返回null。</returns>
        public DbContext GetDbContext(VirtualThing thing)
        {
            var root = thing.GetRoot();
            if (root is null) return null;   //若找不到根
            if (root is not VirtualThing rootThing)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定虚拟对象的根不是 {typeof(VirtualThing)} 类型。");
                return null;
            }
            if (rootThing.RuntimeProperties.GetValueOrDefault(nameof(DbContext)) is not DbContext db)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到对象中存储的数据库上下文属性。Id={rootThing.Id}");
                return null;
            }
            OwHelper.SetLastError(ErrorCodes.NO_ERROR);
            return db;
        }

        #endregion 基础功能

        /// <summary>
        /// 用指定的模板Id创建对象。
        /// </summary>
        /// <param name="tId"></param>
        /// <param name="count">创建多少个对象。</param>
        /// <returns>创建对象的数组，任何创建失败都会导致返回null，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public VirtualThing[] Create(Guid tId, int count)
        {
            var tt = _TemplateManager.Id2FullView.GetValueOrDefault(tId);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定模板，Id={tId}");
                return null;
            }
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
            var type = TemplateManager.GetTypeFromTemplate(tv);    //获取实例类型

            var view = result.GetJsonObject(type);
            _Mapper.Map(tv, view, tv.GetType(), view.GetType()); //复制一般性属性。
            if (tv.Fcps.Count > 0)   //TODO 临时修正
            {
                dynamic dyn = view;
                dyn.Fcps.Clear();
                foreach (var fcps in tv.Fcps)
                    dyn.Fcps.Add(fcps.Key, (FastChangingProperty)fcps.Value.Clone());
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
            var efEntity = context.Entry(thing);
            if (thing.Parent is not null)
            {
                thing.Parent.Children.Remove(thing);
            }
            thing.Parent = null;
            thing.ParentId = null;
            if (efEntity.State != EntityState.Deleted)
                efEntity = context.Remove(thing);
            return efEntity.State == EntityState.Deleted;
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
