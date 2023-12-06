using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 为可快速查找的数据库类提供基础接口。
    /// </summary>
    public interface IDbQuickFind
    {
        /// <summary>
        ///记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        ///常用于记录模板Id或与其它节点的特殊绑定关系，如果没有则是<see cref="Guid.Empty"/>。
        /// </summary>
        /// <remarks><see cref="ExtraGuid"/><see cref="ExtraString"/><see cref="ExtraDecimal"/>三个字段按顺序形成多字段索引以加快搜索速度。
        /// 也创建如下顺序创建索引<see cref="ExtraGuid"/><see cref="ExtraDecimal"/><see cref="ExtraString"/></remarks>
        Guid ExtraGuid { get; set; }

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        [MaxLength(64)]
        string ExtraString { get; set; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        decimal? ExtraDecimal { get; set; }

    }

    /// <summary>
    /// 可快速搜索的对象。
    /// </summary>
    public abstract class DbQuickFindBase : JsonDynamicPropertyBase, IDbQuickFind
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbQuickFindBase()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public DbQuickFindBase(Guid id) : base(id)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>

        public decimal? ExtraDecimal { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Guid ExtraGuid { get; set; }

        private string _ExtraString;
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [MaxLength(64)]
        public string ExtraString { get => _ExtraString; set => _ExtraString = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"><inheritdoc/></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _ExtraString = null;
            }
            base.Dispose(disposing);
        }
    }

    public static class DbTreeNodeExtensions
    {
        /// <summary>
        /// 获取树状结构中指定节点的根节点。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns>树状结构的根节点。如没有父节点则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDbTreeNode<T> GetRoot<T>(this IDbTreeNode<T> node) where T : IEntityWithSingleKey<Guid> =>
            node.GetAncestor(c => c.Parent is null);

        /// <summary>
        /// 在指定节点的祖先(包括双亲,不包括自身)链条上找到第一个符合条件的节点返回。
        /// </summary>
        /// <typeparam name="T"><inheritdoc/></typeparam>
        /// <param name="node">指定节点。不能为空。</param>
        /// <param name="predicate">条件，第一个返沪true的节点被返回。</param>
        /// <returns><paramref name="predicate"/>返回true的第一个节点被返回。</returns>
        public static IDbTreeNode<T> GetAncestor<T>(this IDbTreeNode<T> node, Predicate<IDbTreeNode<T>> predicate) where T : IEntityWithSingleKey<Guid>
        {
            for (var tmp = node.Parent as IDbTreeNode<T>; tmp is not null; tmp = tmp.Parent as IDbTreeNode<T>)
                if (predicate(tmp)) //若找到符合条件的节点
                    return tmp;
            return default;
        }
    }

    /// <summary>
    /// 可快速搜索的对象。
    /// </summary>
    public abstract class DbQuickFindWithRuntimeDictionaryBase : DbQuickFindBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbQuickFindWithRuntimeDictionaryBase()
        {

        }

        public DbQuickFindWithRuntimeDictionaryBase(Guid id) : base(id)
        {
        }

        [AllowNull]
        private ConcurrentDictionary<string, object> _RuntimeProperties;

        /// <summary>
        /// 存储一些运行时需要用的到的属性，使用者自己定义。
        /// 这些存储的属性不会被持久化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public ConcurrentDictionary<string, object> RuntimeProperties
        {
            get => LazyInitializer.EnsureInitialized(ref _RuntimeProperties);
        }

        /// <summary>
        /// 存储RuntimeProperties属性的后备字段是否已经初始化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public bool IsCreatedOfRuntimeProperties => _RuntimeProperties != null;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"><inheritdoc/></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _RuntimeProperties = null;
            }
            base.Dispose(disposing);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class DbTreeNodeBase<T> : DbQuickFindBase, IDbTreeNode<T> where T : IEntityWithSingleKey<Guid>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbTreeNodeBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public DbTreeNodeBase(Guid id) : base(id)
        {
        }

        #endregion 构造函数

        #region 析构及处置对象相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Parent = default;
                _Children = null;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        #region 数据库属性

        #endregion 数据库属性

        #region 导航属性

        private T _Parent;
        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        public virtual T Parent { get => _Parent; set => _Parent = value; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }


        List<T> _Children;
        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public virtual List<T> Children { get => _Children ??= new List<T>(); set => _Children = value; }

        #endregion 导航属性
    }


}
