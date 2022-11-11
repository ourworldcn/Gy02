using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OwGameDb.User
{
    /// <summary>
    /// 账号信息的数据库存储类。
    /// </summary>
    [Index(nameof(ExtraGuid), nameof(ExtraString), nameof(ExtraDecimal), IsUnique = false)]
    [Index(nameof(ExtraGuid), nameof(ExtraDecimal), nameof(ExtraString), IsUnique = false)]
    public class GameUserDo : GameObjectBase, IDbQuickFind, IEntityWithSingleKey<Guid>
    {
        public GameUserDo()
        {
        }

        public GameUserDo(Guid id) : base(id)
        {
        }

        public Guid ExtraGuid { get; set; }

        /// <summary>
        /// 登录名。
        /// </summary>
        public string ExtraString { get; set; }

        public decimal? ExtraDecimal { get; set; }

        public byte[] BinaryArray { get; set; }

        public override T GetJsonObject<T>()
        {
            var result = base.GetJsonObject<T>();

            return result;
        }

    }
}
