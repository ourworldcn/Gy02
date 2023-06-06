using OW.DDD;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 存储在<see cref="IJsonDynamicProperty"/>中实体对象的基类。
    /// </summary>
    public class GameEntityBase : IEntity, IDisposable, INotifyPropertyChanged
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEntityBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="thing">实体对象存储的基础数据对象。</param>
        public GameEntityBase(object thing)
        {
            _Thing = thing;
        }

        #endregion 构造函数

        /// <summary>
        /// 实体的唯一Id。
        /// </summary>
        [JsonIgnore]
        public Guid Id
        {
            get => Thing is IEntityWithSingleKey<Guid> tmp ? tmp.Id : Guid.Empty;
            set => ((IEntityWithSingleKey<Guid>)Thing).Id = value;
        }

        [JsonIgnore]
        public Guid TemplateId
        {
            get => ((IDbQuickFind)Thing)?.ExtraGuid ?? Guid.Empty;
            set => ((IDbQuickFind)Thing).ExtraGuid = value;
        }

        #region IDisposable接口及相关

        private volatile bool _IsDisposed;

        /// <summary>
        /// 对象是否已经被处置。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public bool IsDisposed
        {
            get => _IsDisposed;
            protected set => _IsDisposed = value;
        }

        /// <summary>
        /// 实际处置当前对象的方法。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Thing = null;
                _ExtensionProperties = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SimpleDynamicPropertyBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关

        /// <summary>
        /// 基础存储的实体，通常是<see cref="DbTreeNodeBase{T}"/> 的对象。
        /// </summary>
        [AllowNull]
        private object _Thing;

        /// <summary>
        /// 所属的数据存储对象。通常是<see cref="DbTreeNodeBase{T}"/> 的对象。
        /// </summary>
        [JsonIgnore]
        public object Thing
        {
            get => _Thing;

            set => _Thing = value;
        }

        Dictionary<string, object> _ExtensionProperties;
        /// <summary>
        /// 反序列化时，可能会在 JSON 中收到不是由目标类型的属性表示的数据。
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionProperties
        {
            get => LazyInitializer.EnsureInitialized(ref _ExtensionProperties);
            set => _ExtensionProperties = value;
        }

        #region 事件相关

        protected bool SetStruct<T>(ref T field, T newValue) where T : struct
        {
            if (!Equals(field, newValue))    //若值不相等
            {
                field = newValue;
                Interlocked.Increment(ref _Seq);
                return true;
            }
            return false;
        }

        protected bool SetClass<T1>(ref T1 field, T1 newValue) where T1 : class
        {
            if (!ReferenceEquals(field, newValue))    //若值不相等
            {
                field = newValue;
                Interlocked.Increment(ref _Seq);
                return true;
            }
            return false;
        }

        volatile int _Seq;

        /// <summary>
        /// 获取对象的序列号，每次属性变化，此序列号会变化。
        /// </summary>
        public int Seq => _Seq;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 引发<see cref="PropertyChange"/>事件。
        /// </summary>
        /// <param name="e"><inheritdoc/></param>
        virtual protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            Interlocked.Increment(ref _Seq);
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 引发<see cref="PropertyChange"/>事件。
        /// </summary>
        /// <param name="propertyName"><inheritdoc/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected void InvokeOnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        #endregion 事件相关

        string _Key;
        /// <summary>
        /// 获取该对象的唯一标识。通常该返回值可以用于锁定该对象。
        /// </summary>
        /// <remarks><see cref="Thing"/>如果从<see cref="GuidKeyObjectBase"/>派生则返回<see cref="GuidKeyObjectBase.IdString"/>,否则返回<see cref="Id"/>的全小写字符串形式。
        /// 此属性尽量不生成新的对象。</remarks>
        [JsonIgnore]
        public string Key => Thing is GuidKeyObjectBase gko ? gko.IdString : _Key ??= Id.ToString();
    }
}
