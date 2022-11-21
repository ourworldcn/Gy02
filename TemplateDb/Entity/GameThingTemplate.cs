using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuangYuan.GY001.TemplateDb.Entity
{
    public class GameThingTemplate : JsonDynamicPropertyBase
    {

        public decimal? GId { get; set; }

        public string GenusString { get; set; }

        public string Remark { get; set; }
    }
}
