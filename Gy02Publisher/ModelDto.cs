using OW.Game.Store;
using System.ComponentModel.DataAnnotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using OW.Game.Entity;

namespace Gy02.Publisher
{
    #region 基础数据结构

    public class VirtualThingDto
    {
        public VirtualThingDto(VirtualThing thing)
        {

        }

        /// <summary>
        /// 唯一Id。
        /// </summary>
        public Guid Id { get; set; }

        [AllowNull]
        private byte[] _BinaryArray;
        /// <summary>
        /// 扩展的二进制大对象。
        /// </summary>
        public byte[] BinaryArray
        {
            get { return _BinaryArray; }
            set { _BinaryArray = value; }

        }

        /// <summary>
        /// 所有扩展属性记录在这个字符串中，是一个Json对象。
        /// </summary>
        public string JsonObjectString { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid ExtraGuid { get; set; }

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        public string ExtraString { get; set; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        public decimal? ExtraDecimal { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public List<VirtualThingDto> Children { get; set; } = new List<VirtualThingDto>();

    }

    #endregion 基础数据结构

    #region 账号及登录相关

    public class CreateAccountResultDto
    {
        public CreateAccountResultDto()
        {

        }
        #region 可映射属性

        /// <summary>
        /// 用户登录名。可省略，则自动指定。
        /// </summary>
        [AllowNull]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码，可省略，则自动指定。
        /// </summary>
        [AllowNull]
        public string Pwd { get; set; }

        public VirtualThingDto GameChar { get; set; }

        #endregion 可映射属性
    }

    public class CreateAccountParamsDto
    {
        public CreateAccountParamsDto()
        {
        }
        #region 可映射属性

        /// <summary>
        /// 用户登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }

        #endregion 可映射属性
    }

    public class LoginParamsDto
    {
        public LoginParamsDto()
        {

        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }

        #endregion 可映射属性
    }

    public class LoginReturnDto
    {
        #region 可映射属性

        /// <summary>
        /// 角色的信息。
        /// </summary>
        public GameChar GameChar { get; set; }

        #endregion 可映射属性
    }
    #endregion 账号及登录相关
}