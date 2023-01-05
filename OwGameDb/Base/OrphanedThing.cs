using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 孤立的对象。
    /// </summary>
    [Index(nameof(ExtraGuid), nameof(ExtraString), nameof(ExtraDecimal))]
    [Index(nameof(ExtraGuid), nameof(ExtraDecimal), nameof(ExtraString))]
    public class OrphanedThing : DbQuickFindBase
    {
        [AllowNull]
        private ConcurrentDictionary<string, object> _RuntimeProperties;

        /// <summary>
        /// 存储RuntimeProperties属性的后备字段是否已经初始化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public bool IsCreatedOfRuntimeProperties => _RuntimeProperties != null;

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
        /// 时间戳。
        /// </summary>
        [Timestamp]
        [JsonIgnore]
        public byte[] Timestamp { get; set; }
    }
}