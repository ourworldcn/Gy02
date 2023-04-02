using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 游戏内非容器的虚拟物的实体基类。
    /// </summary>
    public class GameEntity : OwGameEntityBase
    {
        public GameEntity()
        {

        }

        public GameEntity(object thing) : base(thing)
        {
        }

        [JsonPropertyName("lv")]
        public decimal Level { get; set; }

        [JsonPropertyName("count")]
        public decimal Count { get; set; }

    }

    public class GameContainer : GameEntity
    {
        public GameContainer()
        {
        }

        public GameContainer(object thing) : base(thing)
        {
        }

        /// <summary>
        /// 容器的容量。
        /// </summary>
        [JsonPropertyName("cap")]
        public decimal Capacity { get; set; } = -1;
    }
}
