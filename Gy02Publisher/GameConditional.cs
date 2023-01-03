using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using OW.Game.Store;
using OW.Game.Managers;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;

#if NETCOREAPP3_0_OR_GREATER && !NET5_0_OR_GREATER
#warning   大量使用低效Json序列化和反序列化功能，可能导致性能极低;
#endif

namespace OW.Game.Conditional
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGamePrecondition
    {
        /// <summary>
        /// 获取一个指示，确定指定对象是否符合条件。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        bool Match(object obj, IServiceProvider service);
    }

    /// <summary>
    /// 选择一个属性作为前提。
    /// </summary>
    public class GamePropertyCondition : IGamePrecondition
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GamePropertyCondition()
        {

        }

        /// <summary>
        /// 操作符。
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 属性名。
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 属性的值。只能接受数值或其数组类型。
        /// 如果是一个 数值型数组 ,则按级别比对。
        /// 如[1,2,3]则按物品级别lv来选取值。
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 是不是一个空的结构。
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Operator);

#if NETCOREAPP3_0_OR_GREATER    //需要NETCORE 3.0或以上
        private object _InnerValue;

        /// <summary>
        /// 解析<see cref="Value"/>。
        /// 只能接受数值或其数组类型。
        /// </summary>
        [JsonIgnore]
        protected object InnerValue
        {
            get
            {
                if (_InnerValue is null)    //若需初始化
                {
                    lock (this) //避免并发争用
                    {
                        if (_InnerValue is null)    //二相判断
                            if (Value is JsonElement je)
                            {
                                if (je.ValueKind == JsonValueKind.Array)    //若是数组
                                {
                                    _InnerValue = je.EnumerateArray().Select(c => c.GetDecimal()).ToArray();
                                }
                                else if (je.ValueKind == JsonValueKind.Number)
                                    _InnerValue = je.GetDecimal();
                                else
                                    throw new ArgumentException($"只能接受数值或其数组,但遇到未知情况——{je.GetRawText()}");

                            }
                            else
                                _InnerValue = (decimal)Value;
                    }
                }
                return _InnerValue;
            }
        }

        Type _LastType;
        PropertyInfo _PropertyInfo;
        /// <summary>
        /// 级别的属性访问器。
        /// </summary>
        PropertyInfo _LevelPropertyInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PropertyInfo GetPropertyInfo(object obj)
        {
            var type = obj.GetType();
            if (type != _LastType || _PropertyInfo is null)
            {
                lock (this)
                {
                    _LastType = type;
                    _PropertyInfo = _LastType.GetProperty(PropertyName);
                    _LevelPropertyInfo = null;
                }
            }
            return _PropertyInfo;
        }

        /// <summary>
        /// 获取等级数值。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetLevel(object obj)
        {
            var type = obj.GetType();
            if (type != _LastType || _LevelPropertyInfo is null)
            {
                lock (this)
                {
                    _LastType = type;
                    _LevelPropertyInfo = _LastType.GetProperty("lv");
                    _PropertyInfo = null;
                }
            }
            return (int)(_LevelPropertyInfo?.GetValue(obj) ?? 0);
        }

        /// <summary>
        /// 获取一个指示，确定指定对象是否符合条件。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Match(object obj, IServiceProvider service)
        {
            var pi = GetPropertyInfo(obj);
            var value = (decimal)pi.GetValue(obj);
            decimal dec;
            if (InnerValue is decimal[] ary)
            {
                var lv = GetLevel(obj);
                dec = ary[lv];
            }
            else
                dec = (decimal)InnerValue;
            bool result;
            switch (Operator)
            {
                case ">=":
                    result = value >= dec;
                    break;
                case ">":
                    result = value > dec;
                    break;
                case "==":
                    result = value == dec;
                    break;
                case "!=":
                case "<>":
                    result = value != dec;
                    break;
                case "<":
                    result = value < dec;
                    break;
                case "<=":
                    result = value <= dec;
                    break;
                default:
                    throw new ArgumentException($"不认识的比较运算符——{Operator}");
            }
            return result;
        }
#endif
    }

    /// <summary>
    /// 寻找一个物品的条件对象。
    /// </summary>
    public class GameThingPrecondition : IGamePrecondition
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingPrecondition()
        {

        }

        /// <summary>
        /// 父容器的模板Id。
        /// 省略表示不限制。
        /// </summary>
        public Guid? PTId { get; set; }

        /// <summary>
        /// 对象的模板Id。
        /// 省略表示不限制。
        /// </summary>
        public Guid? TId { get; set; }

        /// <summary>
        /// 属的限制。空集合表示不限制，多个属，表示任一个都符合条件。
        /// 此功能当前未实装。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// 属性相关的条件。
        /// 可能是空或空集合，表示不限定具体属性。
        /// </summary>
        public List<GamePropertyCondition> PropertyConditions { get; set; } = new List<GamePropertyCondition>();

#if NETCOREAPP
        Guid? GetPTId(object obj)
        {
            if (obj is VirtualThingEntityBase vtb)
                return vtb.Thing.Parent.ExtraGuid;
            else if (obj is VirtualThing vt)
                return vt.Parent.ExtraGuid;
            else
                return null;
        }

        private Guid? GetTId(object obj)
        {
            if (obj is VirtualThingEntityBase vtb)
                return vtb.Thing.ExtraGuid;
            else if (obj is VirtualThing vt)
                return vt.ExtraGuid;
            else
                return null;
        }

        private IEnumerable<string> GetGenus(object obj, TemplateManager mng)
        {
            if (obj is VirtualThingEntityBase vtb)
                return mng.GetTemplateFromId(vtb.Thing.ExtraGuid).GetJsonObject<Gy02TemplateJO>().Genus;
            else if (obj is VirtualThing vt)
                return mng.GetTemplateFromId(vt.ExtraGuid).GetJsonObject<Gy02TemplateJO>().Genus;
            else
                return Array.Empty<string>();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Match(object obj, IServiceProvider service)
        {
            if (PTId.HasValue)  //若需要匹配父容器模板Id
            {
                if (GetPTId(obj) != PTId.Value)
                    return false;
            }
            if (TId.HasValue)   //若需要匹配模板Id
            {
                if (GetTId(obj) != TId.Value)
                    return false;
            }
            var mng = service.GetRequiredService<TemplateManager>();
            if (Genus != null && Genus.Count > 0)   //若需匹配类属
            {
                var genus = GetGenus(obj, mng);
                if (Genus.Intersect(genus).Count() < Genus.Count)   //若不满足条件
                    return false;
            }
            if (PropertyConditions != null && PropertyConditions.Count > 0)
            {
                if (PropertyConditions.Any(c => !c.Match(obj, service)))
                    return false;
            }
            return true;
        }
#endif

    }

}