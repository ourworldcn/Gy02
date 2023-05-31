using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;

namespace OW.Game.Store
{
    /// <summary>
    /// 标识通用的虚拟事物类所实现的接口。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    public interface IVirtualThing<T> : IJsonDynamicProperty, IDbQuickFind, IDbTreeNode<T>, IEntityWithSingleKey<Guid> where T : IEntityWithSingleKey<Guid>
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class VirtualThingBase<T> : DbTreeNodeBase<T>, IVirtualThing<T> where T : GuidKeyObjectBase
    {
        #region 构造函数

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public VirtualThingBase()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public VirtualThingBase(Guid id) : base(id)
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
                _RuntimeProperties = null;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        #region RuntimeProperties属性相关

        [AllowNull]
        private ConcurrentDictionary<string, object> _RuntimeProperties;

        /// <summary>
        /// 存储一些运行时需要用的到的属性，使用者自己定义。
        /// 这些存储的属性不会被持久化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public ConcurrentDictionary<string, object> RuntimeProperties
        {
            get
            {
                if (_RuntimeProperties is null)
                    Interlocked.CompareExchange(ref _RuntimeProperties, new ConcurrentDictionary<string, object>(), null);
                return _RuntimeProperties;
            }
        }

        /// <summary>
        /// 存储RuntimeProperties属性的后备字段是否已经初始化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public bool IsCreatedOfRuntimeProperties => _RuntimeProperties != null;

        #endregion RuntimeProperties属性相关
    }

    /// <summary>
    /// 存储游戏世界事物的基本类。一般认为他们具有树状结构。
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class VirtualThing : VirtualThingBase<VirtualThing>,IValidatableObject
    {
        #region 构造函数

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public VirtualThing()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public VirtualThing(Guid id) : base(id)
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
                _BinaryArray = default;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        /// <summary>
        /// 时间戳。
        /// </summary>
        [Timestamp]
        [JsonIgnore]
        public byte[] Timestamp { get; set; }

        [AllowNull]
        private byte[] _BinaryArray;
        /// <summary>
        /// 扩展的二进制大对象。
        /// </summary>
        public byte[] BinaryArray
        {
            get { return _BinaryArray; }
            set { _BinaryArray = value; }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override object GetJsonObject(Type type)
        {
            var result = base.GetJsonObject(type);
            if (result is OwGameEntityBase viewBase)
                viewBase.Thing = this;
            return result;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }

        public override string ToString()
        {
            dynamic tt = RuntimeProperties.GetValueOrDefault("Template");
            string name = null;
            try
            {
                name = tt?.DisplayName;
            }
            catch (Exception) { }
            return $"{base.ToString()}({name})";
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return default;
        }
    }
}
