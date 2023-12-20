using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Data
{
    public class GameThingTemplate : JsonDynamicPropertyBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingTemplate()
        {

        }

        [Column(Order = 21)]
        [Comment("扩展的长整型信息。")]
        public long? ExtraLong { get; set; }

        [Column(Order = 90)]
        [Comment("注释信息，服务器不使用该字段。")]
        public string Remark { get; set; }
    }

}
