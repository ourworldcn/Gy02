using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 游戏内槽数据结构。
    /// </summary>
    /// <typeparam name="T">槽内数据类型。</typeparam>
    [Guid("57EB05BD-B8BE-47D5-ADFE-C8C2E8888E38")]
    public class GameSlot<T> : GameEntity where T : new()
    {
        /// <summary>
        /// 槽的容量。
        /// </summary>
        public decimal Capacity { get; set; }

        ICollection<T> _Children;
        /// <summary>
        /// 槽内的道具/装备。
        /// </summary>
        [JsonIgnore]
        public ICollection<T> Children
        {
            get
            {
                if (_Children is null)
                {
                    var tmp = (Thing as VirtualThing)?.Children.Select(c => c.GetJsonObject<T>())?.ToList();
                    Interlocked.CompareExchange(ref _Children, tmp, null);
                }
                return _Children;
            }
        }
    }
}
