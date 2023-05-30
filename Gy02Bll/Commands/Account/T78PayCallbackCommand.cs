using OW.SyncCommand;

namespace GY02.Commands
{
#pragma warning disable IDE1006 // 命名样式
    #region 付费回调
    /// <summary>
    /// 充值回调接口接收的参数。
    /// 付费回调的入口参数。
    /// HTTP POST（application/x-www-form-urlencoded）。
    /// </summary>
    public class PayCallbackT78ParamsDto
    {
        public PayCallbackT78ParamsDto()
        {

        }

        /// <summary>
        /// 游戏ID。
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// 渠道ID。
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 游戏包ID。
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 用户ID。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 游戏方的订单ID。
        /// </summary>
        public string cpOrderId { get; set; }

        /// <summary>
        /// 订单Id。
        /// </summary>
        public string bfOrderId { get; set; }

        /// <summary>
        /// 渠道的订单ID。
        /// </summary>
        public string channelOrderId { get; set; }

        /// <summary>
        /// 金额，单位：分。
        /// </summary>
        public int money { get; set; }

        /// <summary>
        /// 支付透参。
        /// </summary>
        public string callbackInfo { get; set; }

        /// <summary>
        /// 0--支付失败
        /// 1—支付成功
        /// </summary>
        public string orderStatus { get; set; }

        /// <summary>
        /// 目前不支持，固定为空字符串。
        /// </summary>
        public string channelInfo { get; set; }

        /// <summary>
        /// 币种。
        /// </summary>
        public string currency { get; set; }


        /// <summary>
        /// 商品id。
        /// </summary>
        public string product_id { get; set; }

        /// <summary>
        /// 区服id。
        /// </summary>
        public string server_id { get; set; }

        /// <summary>
        /// 角色id。
        /// </summary>
        public string game_role_id { get; set; }

        /// <summary>
        /// 时间戳。
        /// </summary>
        public string time { get; set; }

        /// <summary>
        /// 签名。签名算法见下文。
        /// </summary>
        public string sign { get; set; }
    }

    /// <summary>
    /// 付费回调的返回类。
    /// </summary>
    public class PayCallbackT78ReturnDto
    {
        /// <summary>
        /// 0=成功，表示游戏服务器成功接收了该次充值结果通知,注意是0为成功
        /// 1=失败，表示游戏服务器无法接收或识别该次充值结果通知，如：签名检验不正确、游戏服务器接收失败
        /// </summary>
        public int ret { get; set; }
    }

    public class T78PayCallbackCommand : SyncCommandBase
    {
        public PayCallbackT78ParamsDto Params { get; set; }

        /// <summary>
        /// "1"表示沙箱；其他表示正式。
        /// </summary>
        public string SandBox { get; set; }

        /// <summary>
        /// 支付方式：
        /// "mycard"表示mycard，"google"表示google-play支付，"mol"表示mol支付，"apple"表示苹果支付，“onestore”韩国onestore商店支付，“samsung”三星支付
        /// </summary>
        public string PayType { get; set; }
    }

    #endregion 付费回调

#pragma warning restore IDE1006 // 命名样式

}
