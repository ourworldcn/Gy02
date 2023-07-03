using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Base
{
    /// <summary>
    /// 游戏实体类的扩展方法封装类。
    /// </summary>
    public static class GameEntityExtensions
    {

        public static TemplateStringFullView GetTemplate(this GameEntityBase entity) => entity.GetThing()?.GetTemplate();

        public static void SetTemplate(this GameEntityBase entity, TemplateStringFullView tfv) => entity.GetThing().SetTemplate(tfv);

        /// <summary>
        /// 获取实体对象宿主对象。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>宿主对象，如果没有找到则返回null。</returns>
        public static VirtualThing GetThing(this GameEntityBase entity)
        {
            return entity.Thing as VirtualThing;
        }

        /// <summary>
        /// 设置指定属性的值。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="pi"></param>
        /// <param name="difference">差值，正数增加，负数减少。</param>
        /// <param name="changes"></param>
        public static void SetPropertyValue(this GameEntity entity, PropertyDescriptor pi, decimal difference, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var oldVal = Convert.ToDecimal(pi.GetValue(entity));   //旧属性值
            var newVal = oldVal + difference; //新属性值
            pi.SetValue(entity, newVal);    //设置属性值
            changes?.Add(new GamePropertyChangeItem<object>
            {
                Object = entity,
                PropertyName = pi.Name,
                OldValue = oldVal,
                HasOldValue = true,
                NewValue = newVal,
                HasNewValue = true,
            });
        }

        public static bool ModifyPropertyAndMarkChanges<T>(this GameEntityBase entityBase, string propertyName, T newValue, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var pi = entityBase.GetType().GetProperty(propertyName);
            if (pi is null) //若找不到指定属性的访问器
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到指定属性，Id={entityBase.Id} ,{nameof(propertyName)}=\"{propertyName}\"");
                return false;
            }
            if (!pi.CanWrite || !pi.CanRead)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"指定属性必须能读写，Id={entityBase.Id} ,{nameof(propertyName)}=\"{propertyName}\"");
                return false;
            }
            var oldValue = (T)pi.GetValue(entityBase);
            pi.SetValue(entityBase, newValue);
            changes.Add(new GamePropertyChangeItem<object>
            {
                Object = entityBase,
                PropertyName = propertyName,

                HasOldValue = typeof(T).IsClass && oldValue != null || typeof(T).IsValueType,
                OldValue = oldValue,

                HasNewValue = true,
                NewValue = newValue,

            });
            return true;


        }

    }
}
