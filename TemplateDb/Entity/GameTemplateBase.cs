/*物品对象
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.TemplateDb.Entity
{
    /// <summary>
    /// 虚拟事物对象的模板类。
    /// </summary>
    public abstract class GameThingTemplateBase : GameTemplateBase
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameThingTemplateBase()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameThingTemplateBase(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 显示名称。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 该模板创建对象应有的子模板Id字符串集合。用逗号分割。
        /// 可能存在多个相同Id。
        /// </summary>
        public string ChildrenTemplateIdString { get; set; }

        /// <summary>
        /// 脚本，内容根据使用情况具体定义。
        /// </summary>
        public string Script { get; set; }

        private List<Guid> _ChildrenTemplateIds;

        /// <summary>
        /// 该模板创建对象应有的子模板Id集合。
        /// 可能存在多个相同Id。
        /// </summary>
        [NotMapped]
        public List<Guid> ChildrenTemplateIds
        {
            get
            {
                if (_ChildrenTemplateIds is null)
                    lock (this)
                        if (_ChildrenTemplateIds is null)
                        {
                            if (string.IsNullOrWhiteSpace(ChildrenTemplateIdString))
                            {
                                _ChildrenTemplateIds = new List<Guid>();
                            }
                            else
                            {
                                _ChildrenTemplateIds = ChildrenTemplateIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                            }
                        }
                return _ChildrenTemplateIds;
            }
        }

        #region 序列属性相关


        #endregion 序列属性相关

    }

    public static class GameThingTemplateBaseExtensons
    {
    }
}
