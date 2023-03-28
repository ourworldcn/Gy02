using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 道具类数据结构。
    /// </summary>
    [Guid("F3F9B16B-499E-4228-AEC0-FFA5B9F18E9B")]
    public class GameItem: GameEntity
    {
        public GameItem() { }

        public GameItem(object thing) : base(thing)
        {
        }

    }
}
