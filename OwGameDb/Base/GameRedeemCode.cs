using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Store.Base
{
    /// <summary>
    /// 兑换码表。
    /// </summary>
    public class GameRedeemCode
    {
        public GameRedeemCode()
        {
        }

        /// <summary>
        /// 兑换码的明文。
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
        [MaxLength(64)]
        [Comment("兑换码，也是Id。")]
        public string Code { get; set; }

        /// <summary>
        /// 兑换码所属批次/类型。
        /// </summary>
        public Guid CatalogId { get; set; }

        /// <summary>
        /// 已经兑换的次数。
        /// </summary>
        public int Count { get; set; } = 0;
    }

    /// <summary>
    /// 兑换码的批次表。
    /// </summary>
    public class GameRedeemCodeCatalog : GuidKeyObjectBase
    {
        public GameRedeemCodeCatalog()
        {
        }

        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("显示名称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 生成的码的类型，1=通用码，2=一次性码。
        /// </summary>
        [Comment("生成的码的类型，1=通用码，2=一次性码。")]
        public int CodeType { get; set; }

        /// <summary>
        /// 兑换码使用的商品TId。
        /// </summary>
        [Comment("兑换码使用的商品TId")]
        public Guid ShoppingTId { get; set; }
    }
}
