using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Buffers;
using System.Transactions;
using System.Runtime.InteropServices;

namespace OW.Game.Entity
{
    /// <summary>
    /// 用户账号类。
    /// </summary>
    [Guid("60DE10B7-8CC6-4D51-B356-691ECBB77C03")]
    public class GameUser : GameEntityBase
    {
        #region 构造函数及相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameUser()
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="thing"><inheritdoc/></param>
        public GameUser(OrphanedThing thing) : base(thing)
        {
            Initialize();
        }

        private void Initialize()
        {
        }

        #endregion 构造函数及相关

        #region 敏感信息

        /// <summary>
        /// 登录名。
        /// </summary>
        [Required]
        [StringLength(64)]
        [JsonIgnore]
        public string LoginName
        {
            get => ((IDbQuickFind)Thing).ExtraString;
            set => ((IDbQuickFind)Thing).ExtraString = value;
        }

        /// <summary>
        /// 密码的Hash值。
        /// </summary>
        [JsonIgnore]
        public byte[] PwdHash { get => ((VirtualThing)Thing).BinaryArray; set => ((VirtualThing)Thing).BinaryArray = value; }

        /// <summary>
        /// 密码是否正确。
        /// </summary>
        /// <param name="pwd">密码明文。</param>
        /// <returns>true密码匹配，false密码不匹配。</returns>
        public bool IsPwd(string pwd)
        {
            if (PwdHash is null && pwd is null)
                return true;
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(pwd ?? string.Empty));
            return hash.SequenceEqual(PwdHash ?? Array.Empty<byte>());
        }

        /// <summary>
        /// 设置密码。
        /// </summary>
        /// <param name="pwd"></param>
        public void SetPwd(string pwd)
        {
            PwdHash = SHA1.HashData(Encoding.UTF8.GetBytes(pwd));
            Dan = pwd;
        }

        /// <summary>
        /// 临时加入的属性。用于记录一些测试时需要的信息。
        /// </summary>
        public string Dan { get; set; }

        /// <summary>
        /// 当前承载此用户的服务器节点号。空则表示此用户尚未被任何节点承载（未在线）。但有节点号，不代表用户登录，可能只是维护等其他目的将用户承载到服务器中。
        /// </summary>
        [JsonIgnore]
        public int? NodeNum
        {
            get => (int?)((VirtualThing)Thing).ExtraDecimal;
            set => ((VirtualThing)Thing).ExtraDecimal = value;
        }

        /// <summary>
        /// 用户 Token。
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public Guid Token { get; set; }

        #endregion 敏感信息

        /// <summary>
        /// 账号所处区域。目前可能值是IOS或Android。
        /// </summary>
        [StringLength(64)]
        public string Region { get; set; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = OwHelper.WorldNow;

        #region 非数据库属性

        /// <summary>
        /// 最后一次操作的时间。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public DateTime LastModifyDateTimeUtc { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 超时时间。
        /// </summary>
        /// <value>默认值15分钟。</value>
        [NotMapped]
        [JsonConverter(typeof(TimeSpanJsonConverter))]
        [JsonIgnore]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

        #endregion 非数据库属性

        #region 导航属性

        /// <summary>
        /// <see cref="GameChars"/>属性的后备字段。
        /// </summary>
        private List<GameChar> _GameChars = new List<GameChar>();

        /// <summary>
        /// 导航到多个角色的属性。
        /// </summary>
        [JsonIgnore]
        public virtual List<GameChar> GameChars
        {
            get => _GameChars;
            set => _GameChars = value;
        }

        /// <summary>
        /// 玩家当前使用的角色。
        /// 选择当前角色后，需要设置该属性。
        /// </summary>
        /// <value>当前角色对象，null用户尚未选择角色。</value>
        [NotMapped]
        [JsonIgnore]
        public GameChar CurrentChar
        {
            get => ((VirtualThing)Thing).RuntimeProperties.GetValueOrDefault(nameof(CurrentChar)) as GameChar;
            set => ((VirtualThing)Thing).RuntimeProperties[nameof(CurrentChar)] = value;
        }

        #endregion 导航属性

        #region 扩展属性

        /// <summary>
        /// 禁言到期时间。空表示没有禁言。
        /// </summary>
#if NETCOREAPP
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public DateTime? SilenceUtc { get; set; }

        /// <summary>
        /// 封停账号到期时间。空表示没有被封停。
        /// </summary>
#if NETCOREAPP
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public DateTime? BlockUtc { get; set; }

        #endregion 扩展属性

        #region IDisposable 接口相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (!IsDisposed)
        //    {
        //        if (disposing)
        //        {
        //            // 释放托管状态(托管对象)
        //            //CurrentChar?.Dispose();
        //        }

        //        // 释放未托管的资源(未托管的对象)并重写终结器
        //        // 将大型字段设置为 null
        //        CurrentChar = null;
        //        _GameChars = null;
        //        base.Dispose(disposing);
        //    }

        //}

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameUser()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable 接口相关

        #region 事件

        #endregion 事件
    }

    public static class GameUserExtensions
    {
        /// <summary>
        /// 记录服务提供者。
        /// </summary>
        public static IServiceProvider GetServices(this GameUser user)
        {
            return ((VirtualThing)user.Thing).RuntimeProperties.GetValueOrDefault("Services") as IServiceProvider;
        }

        /// <summary>
        /// 记录服务提供者。
        /// </summary>
        public static void SetServices(this GameUser user, IServiceProvider value)
        {
            ((VirtualThing)user.Thing).RuntimeProperties["Services"] = value;
        }

        /// <summary>
        /// 管理该用户数据存储的上下文。
        /// </summary>
        /// <param name="user"></param>
        public static DbContext GetDbContext(this GameUser user)
        {
            return ((VirtualThing)user.Thing).GetDbContext();
        }

        /// <summary>
        /// 管理该用户数据存储的上下文。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="value"></param>
        public static void SetDbContext(this GameUser user, DbContext value)
        {
            ((VirtualThing)user.Thing).SetDbContext(value);
        }

    }
}
