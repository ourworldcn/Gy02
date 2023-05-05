using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OW.Game
{
    public class DataObjectManagerOptions : IOptions<DataObjectManagerOptions>
    {
        public DataObjectManagerOptions Value => this;

        /// <summary>
        /// 锁定键的默认超时。
        /// </summary>
        /// <value>默认值:3秒钟。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// 扫描间隔。
        /// </summary>
        /// <value>默认值:1分钟。</value>
        public TimeSpan ScanFrequency { get; internal set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 默认的缓存超时。
        /// </summary>
        /// <value>默认值:1分钟。</value>
        public TimeSpan DefaultCachingTimeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 数据对象设置。
    /// </summary>
    public class DataObjectOptions
    {
        public DataObjectOptions()
        {
        }

        public string Key { get; set; }

        /// <summary>
        /// 加载时调用。
        /// 在对键加锁的范围内调用。
        /// </summary>
        [AllowNull]
        public Func<string, object, object> LoadCallback { get; set; }

        /// <summary>
        /// <see cref="LoadCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object LoadCallbackState { get; set; }

        /// <summary>
        /// 需要保存时调用。
        /// 在对键加锁的范围内调用。
        /// 回调参数是要保存的对象，附加数据，返回true表示成功，否则是没有保存成功,若没有设置该回调，则说民无需保存，也就视同保存成功。
        /// </summary>
        [AllowNull]
        public Func<object, object, bool> SaveCallback { get; set; }

        /// <summary>
        /// <see cref="SaveCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object SaveCallbackState { get; set; }

        /// <summary>
        /// 从缓存中移除后调用。
        /// 在对键加锁的范围内。
        /// </summary>
        public Action<object, object> LeaveCallback { get; set; }

        /// <summary>
        /// <see cref="LeaveCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object LeaveCallbackState { get; set; }

        /// <summary>
        /// 从对象获取字符串类型键的函数，默认<see cref="GuidKeyObjectBase.IdString"/>。如果不是该类型或派生对象，请设置这个成员。
        /// </summary>
        public Func<object, string> GetKeyCallback { get; set; } = c => ((GuidKeyObjectBase)c).IdString;

    }

    public class DataObjectItemEntry
    {

        public DataObjectItemEntry(object key)
        {
            Key = key;
        }

        public object Key { get; set; }

        /// <summary>
        /// 需要保存时调用。
        /// 在对键加锁的范围内调用。
        /// 回调参数是要保存的对象，附加数据，返回true表示成功，否则是没有保存成功,若没有设置该回调，则说明无需保存，也就视同保存成功。
        /// (value,state)
        /// </summary>
        [AllowNull]
        public Func<object, object, bool> SaveCallback { get; set; }

        /// <summary>
        /// <see cref="SaveCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object SaveCallbackState { get; set; }

        //#region ICacheEntry接口及相关

        //public OwMemoryCache.OwMemoryCacheEntry CacheEntry { get; set; }
        //public object Value { get => ((ICacheEntry)CacheEntry).Value; set => ((ICacheEntry)CacheEntry).Value = value; }
        //public DateTimeOffset? AbsoluteExpiration { get => ((ICacheEntry)CacheEntry).AbsoluteExpiration; set => ((ICacheEntry)CacheEntry).AbsoluteExpiration = value; }
        //public TimeSpan? AbsoluteExpirationRelativeToNow { get => ((ICacheEntry)CacheEntry).AbsoluteExpirationRelativeToNow; set => ((ICacheEntry)CacheEntry).AbsoluteExpirationRelativeToNow = value; }
        //public TimeSpan? SlidingExpiration { get => ((ICacheEntry)CacheEntry).SlidingExpiration; set => ((ICacheEntry)CacheEntry).SlidingExpiration = value; }

        //public IList<IChangeToken> ExpirationTokens => ((ICacheEntry)CacheEntry).ExpirationTokens;

        //public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => ((ICacheEntry)CacheEntry).PostEvictionCallbacks;

        //public CacheItemPriority Priority { get => ((ICacheEntry)CacheEntry).Priority; set => ((ICacheEntry)CacheEntry).Priority = value; }
        //public long? Size { get => ((ICacheEntry)CacheEntry).Size; set => ((ICacheEntry)CacheEntry).Size = value; }

        //public void Dispose()
        //{
        //    ((IDisposable)CacheEntry).Dispose();
        //}
        //#endregion ICacheEntry接口及相关
    }

    /// <summary>
    /// 数据对象的管理器，负责单例加载，保存并自动驱逐。
    /// </summary>
    public class DataObjectManager
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options"></param>
        public DataObjectManager(IOptions<DataObjectManagerOptions> options, IServiceProvider service)
        {
            Options = options.Value;
            _Service = service;
            Initialize();
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        void Initialize()
        {
            _Timer = new Timer(TimerCallback, null, Options.ScanFrequency, Options.ScanFrequency);
        }

        #endregion 构造函数

        OwMemoryCache _Cache = new OwMemoryCache(new OwMemoryCacheOptions());
        /// <summary>
        /// 缓存对象。
        /// </summary>
        internal OwMemoryCache Cache => _Cache;

        ConcurrentDictionary<object, DataObjectItemEntry> _Items = new ConcurrentDictionary<object, DataObjectItemEntry>();
        /// <summary>
        /// 配置项。
        /// </summary>
        public IReadOnlyDictionary<object, DataObjectItemEntry> Items => _Items;

        DataObjectManagerOptions _Options;

        public DataObjectManagerOptions Options { get => _Options; set => _Options = value; }

        /// <summary>
        /// 存储服务容器。
        /// </summary>
        private IServiceProvider _Service;


        /// <summary>
        /// 存储优先要从要存储的对象的键。仅使用键来存储。
        /// </summary>
        HashSet<object> _Dirty = new HashSet<object>();

        /// <summary>
        /// 锁定指定键对象，以备进行操作。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool TryEnter(object key, TimeSpan timeout) => _Cache.TryEnter(key, timeout);

        /// <summary>
        /// 释放锁定的键。
        /// </summary>
        /// <param name="key"></param>
        public void Exit(object key) => _Cache.Exit(key);

        /// <summary>
        /// 返回缓存对象或加载后返回。只有在加载过程中才会锁定键且在返回之前会解锁，如果需要锁定，调用者可以提前锁定键。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="setter">如果指定键不在管理器内，则通过此回调生成对象。</param>
        /// <returns></returns>
        public T GetOrLoad<T>(object key, Func<DataObjectItemEntry, OwMemoryCache.OwMemoryCacheEntry, object> setter) where T : class
        {
            if (_Cache.Items.TryGetValue(key, out var val)) //若找到了该键的对象
                return (T)val.Value;
            T result;

            using var dw = DisposeHelper.Create(_Cache.TryEnter, _Cache.Exit, key, _Cache.Options.DefaultLockTimeout);
            if (dw.IsEmpty) //若无法锁定键值
            {
                result = default;
                return result;
            }
            var itemEntry = new DataObjectItemEntry(key)
            {
            };
            var entry = _Cache.CreateEntry(key);
            entry.Value = setter(itemEntry, (OwMemoryCache.OwMemoryCacheEntry)entry);
            result = entry.Value as T;
            if (entry.Value != null)
            {
                entry.Dispose();
                _Items.TryAdd(key, itemEntry);
            }
            return result;
        }

        /// <summary>
        /// 在本地中获取对象，如果没有则返回null。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Find(string key)
        {
            using var dw = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
                return null;
            return _Cache.TryGetValue(key, out var result) ? result : default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="initializer"></param>
        /// <returns></returns>
        public bool TryAdd(object key, object value, Action<DataObjectItemEntry, OwMemoryCache.OwMemoryCacheEntry> initializer)
        {
            using var dw = DisposeHelper.Create(TryEnter, Exit, key, _Options.DefaultLockTimeout);
            if (dw.IsEmpty)  //若无法锁定键
                return false;
            if (_Cache.Items.ContainsKey(key))   //若键已经存在
                return false;
            var itemEntry = new DataObjectItemEntry(key)
            {
            };
            using (var entry = _Cache.CreateEntry(key))
            {
                entry.Value = value;
                initializer(itemEntry, (OwMemoryCache.OwMemoryCacheEntry)entry);
            }
            return _Items.TryAdd(key, itemEntry);
        }

        /// <summary>
        /// 移除指定项，自动调用保存。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual bool TryRemoveCore(object key, out object result)
        {
            if (!_Items.Remove(key, out var item))
            {
                result = default;
                return false;
            }
            if (!_Cache.Items.TryGetValue(key, out var entry))
            {
                result = default;
                return false;
            }
            //item.SaveCallback?.Invoke(entry.Value, item.SaveCallbackState);
            result = entry.Value;
            _Cache.Remove(key);
            return true;
        }

        /// <summary>
        /// 移除项。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryRemove(object key)
        {
            using var dw = DisposeHelper.Create(TryEnter, Exit, key, _Options.DefaultLockTimeout);
            if (dw.IsEmpty)  //若无法锁定键
                return false;
            if (!_Cache.Items.ContainsKey(key))   //若键已经不存在
                return false;
            return TryRemoveCore(key, out var result);
        }

        /// <summary>
        /// 指出对象已经更改，需要保存。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool SetDirty(object key)
        {
            lock (_Dirty)
                return _Dirty.Add(key);
        }

        #region 后台工作相关

        /// <summary>
        /// 定时器。
        /// </summary>
        Timer _Timer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void TimerCallback(object state)
        {
            _Cache.Compact();
            Save();
        }

        /// <summary>
        /// 对标记为脏的数据进行保存。
        /// </summary>
        protected void Save()
        {
            List<object> keys = AutoClearPool<List<object>>.Shared.Get();
            using var dwKeys = DisposeHelper.Create(c => AutoClearPool<List<object>>.Shared.Return(c), keys);
            lock (_Dirty)
            {
                OwHelper.Copy(_Dirty, keys);
                _Dirty.Clear();
            }
            for (int i = keys.Count - 1; i >= 0; i--)   //逆序遍历，略微提高性能
            {
                var key = keys[i];
                using var dw = DisposeHelper.Create(TryEnter, Exit, key, TimeSpan.Zero);
                if (dw.IsEmpty)
                    continue;
                if (!_Items.TryGetValue(key, out var item) || !_Cache.Items.TryGetValue(key, out var entry))  //若键下的数据已经销毁
                {
                    keys.RemoveAt(i);
                    continue;
                }
                try
                {
                    if (item.SaveCallback?.Invoke(entry.Value, item.SaveCallbackState) ?? true)  //若正常存储
                        keys.RemoveAt(i);
                }
                catch (Exception)
                {
                }
            }
            //放入下次再保存
            if (keys.Count > 0)
                lock (_Dirty)
                    OwHelper.Copy(keys, _Dirty);
        }

        #endregion 后台工作相关

        //public bool SetTimout(string key, TimeSpan timeout)
        //{
        //    using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.Timeout);
        //    if (dw.IsEmpty)
        //    {
        //        return false;
        //    }
        //    if (!_Datas.TryGetValue(key, out var entity))
        //    {
        //        return false;
        //    }
        //    entity.Timeout = timeout;
        //    return true;
        //}

    }

    public static class DataObjectManagerExtensions
    {
        /// <summary>
        /// 设置加载回调和参数。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataObjectCache.DataObjectCacheEntry SetLoadCallback(this DataObjectCache.DataObjectCacheEntry entry, Func<object, object, object> callback, object state)
        {
            entry.LoadCallbackState = state;
            entry.LoadCallback = callback;
            return entry;
        }

        /// <summary>
        /// 设置保存回调和参数。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataObjectCache.DataObjectCacheEntry SetSaveCallback(this DataObjectCache.DataObjectCacheEntry entry, Func<object, object, bool> callback, object state)
        {
            entry.SaveCallbackState = state;
            entry.SaveCallback = callback;
            return entry;
        }

        /// <summary>
        /// 设置创建对象的回调和参数。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="callback">(键，用户状态对象)，返回是要缓存的对象。</param>
        /// <param name="state"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataObjectCache.DataObjectCacheEntry SetCreateCallback(this DataObjectCache.DataObjectCacheEntry entry, Func<object, object, object> callback, object state)
        {
            entry.CreateCallbackState = state;
            entry.CreateCallback = callback;
            return entry;
        }

        /// <summary>
        /// 注册驱逐前回调。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static DataObjectCache.DataObjectCacheEntry RegisterBeforeEvictionCallback(this DataObjectCache.DataObjectCacheEntry entry, Action<object, object, EvictionReason, object> callback, object state = null)
        //{
        //    entry.BeforeEvictionCallbacks.Add(new BeforeEvictionCallbackRegistration() { BeforeEvictionCallback = callback, State = state });
        //    return entry;
        //}

    }
}
