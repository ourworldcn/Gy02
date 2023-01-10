using Microsoft.EntityFrameworkCore;
using OW.DDD;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 孤立的对象。
    /// </summary>
    [Index(nameof(ExtraGuid), nameof(ExtraString), nameof(ExtraDecimal))]
    [Index(nameof(ExtraGuid), nameof(ExtraDecimal), nameof(ExtraString))]
    public class OrphanedThing : DbQuickFindWithRuntimeDictionaryBase
    {

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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T GetJsonObject<T>()
        {
            var result = base.GetJsonObject<T>();
            if (result is OwGameEntityBase viewBase)
                viewBase.Thing = this;
            return result;
        }

    }

}