using Microsoft.EntityFrameworkCore;
using OW.Data;
using OW.Game.Store;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace OW.GameDb
{
    /// <summary>
    /// 关键行为记录类。
    /// 此类可能放在玩家数据库中也可能放于专用的日志库中，但可能有些游戏内操作需要此数据。
    /// 当前没有启动第三上下文，暂时放在玩家数据库中。
    /// </summary>
    /// <remarks>
    /// <code>
    /// IQueryable<GameActionRecord> query; //一个查询对象
    /// DateTime dt = OwHelper.WorldClock.Date;
    /// Guid charId = Guid.NewGuid();
    /// string actionId = "someThing";
    /// var coll = query.Where(c => c.DateTimeUtc >= dt && c.Id == charId && c.ActionId == actionId);
    /// </code>
    /// 索引在此情况下最有用。
    /// </remarks>
    [Index(nameof(ActionId), nameof(WorldDateTime), IsUnique = false)]
    [Index(nameof(WorldDateTime),nameof(ActionId),  IsUnique = false)]
    public class ActionRecord : JsonDynamicPropertyBase
    {
        public ActionRecord()
        {
        }

        public ActionRecord(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。
        /// </summary>
        [MaxLength(64)]
        [Comment("行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。")]
        public string ActionId { get; set; }

        /// <summary>
        /// 这个行为发生的世界时间。
        /// </summary>
        /// <value>默认是构造此对象的<see cref="OwHelper.WorldNow"/>时间。</value>
        [Comment("这个行为发生的世界时间。")]
        public DateTime WorldDateTime { get; set; } = OwHelper.WorldNow;
        
        /// <summary>
        /// 额外Guid。
        /// </summary>
        [Comment("额外Guid。")]
        public Guid? ExtraGuid { get; set; }

        /// <summary>
        /// 额外的字符串，通常行为Id，最长64字符。
        /// </summary>
        [MaxLength(64)]
        [Comment("额外的字符串，通常行为Id，最长64字符。")]
        public string ExtraString { get; set; }

        /// <summary>
        /// 额外数字，具体意义取决于该条记录的类型。
        /// </summary>
        [Precision(18, 4)]
        [Comment("额外数字，具体意义取决于该条记录的类型。")]
        public decimal ExtraDecimal { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    [Index(nameof(ActionId), nameof(WorldDateTime), IsUnique = false)]
    public class SimpleActionItem : IJsonDynamicProperty
    {
        /// <summary>
        /// 唯一Id。
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Comment("唯一Id")]
        public long Id { get; set; }

        /// <summary>
        /// 行为Id。如ShoppingBuy.xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx,ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==
        /// </summary>
        [MaxLength(64)]
        [Comment("行为Id。如ShoppingBuy.xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx,ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==")]
        public string ActionId { get; set; }

        /// <summary>
        /// 这个行为发生的世界时间。
        /// </summary>
        /// <value>默认是构造此对象的<see cref="OwHelper.WorldNow"/>时间。</value>
        [Comment("这个行为发生的世界时间。")]
        public DateTime WorldDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 额外Guid。
        /// </summary>
        [Comment("额外Guid。")]
        public Guid? ExtraGuid { get; set; }

        /// <summary>
        /// 额外的字符串，通常行为Id，最长64字符。
        /// </summary>
        [MaxLength(64)]
        [Comment("额外的字符串，通常行为Id，最长64字符。")]
        public string ExtraString { get; set; }

        /// <summary>
        /// 额外数字，具体意义取决于该条记录的类型。
        /// </summary>
        [Precision(18, 4)]
        [Comment("额外数字，具体意义取决于该条记录的类型。")]
        public decimal ExtraDecimal { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string JsonObjectString { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [NotMapped]
        public Type JsonObjectType { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [NotMapped]
        public object JsonObject { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T GetJsonObject<T>() where T : new()
        {
            return default;
        }
    }
}
