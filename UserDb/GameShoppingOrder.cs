using Microsoft.EntityFrameworkCore;
using OW.Data;
using OW.Game.PropertyChange;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace OW.Game.Store
{
    /// <summary>
    /// 法币购买商品的订单。
    /// </summary>
    [Index(nameof(CustomerId), nameof(CreateUtc))]
    public class GameShoppingOrder : JsonDynamicPropertyBase, ICloneable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameShoppingOrder()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public GameShoppingOrder(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 目前是角色Id的字符串形式。如果以后存在给账号购买的情况则可能是账号Id。
        /// </summary>
        [MaxLength(64)]
        public string CustomerId { get; set; }

        /// <summary>
        /// 订单总金额。注意金额为正是订单，负数是"冲红"单。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 币种。
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// 订单的详细项。
        /// </summary>
        public virtual List<GameShoppingOrderDetail> Detailes { get; set; } = new List<GameShoppingOrderDetail>();

        /// <summary>
        /// 第一方是否已经确认。如客户端。
        /// </summary>
        public bool Confirm1 { get; set; }

        /// <summary>
        /// 第二方是否已经确认。如sdk方。
        /// </summary>
        public bool Confirm2 { get; set; }

        /// <summary>
        /// 状态。0=进行中，1=正常完成，2=多方都已确认，但确认数据不一致，即出错;3至少有一方明确指出失败。
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 附属信息。
        /// </summary>
        public byte[] BinaryArray { get; set; }

        /// <summary>
        /// 创建该订单的世界时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 完成的时间。
        /// </summary>
        public DateTime? CompletionDateTime { get; set; }

        /// <summary>
        /// 获取一个深表副本。注意Id也被复制，通常需要调用<see cref="GuidKeyObjectBase.GenerateNewId"/>换成新Id。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new GameShoppingOrder
            {
                Amount = Amount,
                Currency = Currency,
                BinaryArray = BinaryArray.ToArray(),
                Confirm1 = Confirm1,
                Confirm2 = Confirm2,
                CustomerId = CustomerId,
                Id = Id,
                JsonObjectString = JsonObjectString,
                CreateUtc = CreateUtc,
                CompletionDateTime = CompletionDateTime,
            };
            Detailes.ForEach(c => result.Detailes.Add((GameShoppingOrderDetail)c.Clone()));
            return result;
        }



        /// <summary>
        /// 将对象和子对象的Id换成新Id。通常在<see cref="Clone"/>后调用。
        /// </summary>
        public void ChangeNewId()
        {
            GenerateNewId();
            Detailes.ForEach(c => c.GenerateNewId());
        }

    }

    /// <summary>
    /// 法币购买商品的订单的详细项。目前情况，往往一个订单只有一项。
    /// </summary>
    public class GameShoppingOrderDetail : GuidKeyObjectBase, ICloneable
    {
        public GameShoppingOrderDetail()
        {

        }

        /// <summary>
        /// 货物Id。商品的Id的字符串形式。
        /// </summary>
        public string GoodsId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        public decimal Count { get; set; }

        /// <summary>
        /// 单价。暂时未用。币种要和订单内币种一致。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 附属信息。
        /// </summary>
        public byte[] BinaryArray { get; set; }

        /// <summary>
        /// 获取一个深表副本。注意Id也被复制，通常需要调用<see cref="GuidKeyObjectBase.GenerateNewId"/>换成新Id。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new GameShoppingOrderDetail
            {
                Id = Id,
                BinaryArray = BinaryArray.ToArray(),
                Count = Count,
                Price = Price,
                GoodsId = GoodsId,
            };
            return result;
        }
    }

}
