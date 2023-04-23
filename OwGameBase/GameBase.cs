/* **********************************************************************
 * 文件放置游戏专用的一些基础类
 * 一些游戏中常用的基础数据结构。
 ********************************************************************** */
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace OW.Game
{
    /// <summary>
    /// 渐变属性封装类。
    /// </summary>
    public class FastChangingProperty : IValidatableObject
    {

        public FastChangingProperty()
        {

        }

        /// <summary>
        /// 构造函数、
        /// </summary>
        /// <param name="delay">计算间隔。</param>
        /// <param name="increment">增量。</param>
        /// <param name="maxVal">最大值。不会超过此值。</param>
        /// <param name="currentVal">当前值。</param>
        /// <param name="lastComputerDateTime">时间。建议一律采用Utc时间。</param>
        public FastChangingProperty(TimeSpan delay, decimal increment, decimal maxVal, decimal currentVal, DateTime lastComputerDateTime)
        {
            if (increment > 0 && maxVal < currentVal || increment < 0 && maxVal > currentVal)  //若不向终值收敛
                Debug.WriteLine("不向终值收敛。");
            _LastValue = currentVal;
            LastDateTime = lastComputerDateTime;
            Delay = delay;
            StepValue = increment;
        }

        #region 属性及相关

        /// <summary>
        /// 起始的时间点。总是从该时间点开始计算。
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 最小值。
        /// </summary>
        public decimal MinValue { get; set; }

        /// <summary>
        /// 最大值。
        /// </summary>
        public decimal MaxValue { get; set; }

        /// <summary>
        /// 每次跳点的增量。可能是正值也可能是负值，正值每次跳变时增加<see cref="CurrentValue"/>，否则减少。
        /// </summary>
        public decimal StepValue { get; set; }

        /// <summary>
        /// 多久计算一次。即每次跳变的时间长度。
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 获取或设置最后计算的结果。<see cref="LastDateTime"/>这个时点上计算的值。
        /// </summary>
        public decimal CurrentValue { get => _LastValue; set => _LastValue = value; }

        /// <summary>
        /// 获取或设置最后计算的时间。建议一律采用Utc时间。默认值是构造时的当前时间。
        /// </summary>
        public DateTime LastDateTime { get; set; } = DateTime.UtcNow;

        private decimal _LastValue;
        /// <summary>
        /// 一个记录额外信息的属性。本类成员不使用该属性。
        /// </summary>
        public object Tag { get; set; }

        #endregion 属性及相关

        /// <summary>
        /// 获取当前值。自动修改<see cref="LastComputerDateTime"/>和<see cref="CurrentValue"/>属性。
        /// </summary>
        /// <param name="now">当前时间。返回时可能更改，如果没有正好到跳变时间，则会提前到上一次跳变的时间点。</param>
        /// <returns>更改后的值(<see cref="CurrentValue"/>)。</returns>
        public decimal GetCurrentValue(ref DateTime now)
        {
            if (StepValue > 0) //若增量跳变
            {
                var count = (long)Math.Round((decimal)(now - LastDateTime).Ticks / Delay.Ticks, MidpointRounding.ToNegativeInfinity);   //跳变次数,回调可能多跳一次
                LastDateTime += Delay * count;
                now = LastDateTime;
                if (_LastValue >= MaxValue)  //若已经结束
                {
                    return _LastValue;
                }
                else //若尚未结束
                {
                    _LastValue = Math.Clamp(_LastValue + count * StepValue, decimal.Zero, MaxValue);
                }
            }
            else if (StepValue < 0) //若减量跳变
            {

            }
            else
                throw new InvalidOperationException("步进值不能是0。");
            return _LastValue;
        }

        /// <summary>
        /// 使用当前Utc时间获取当前值。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetCurrentValueWithUtc()
        {
            DateTime now = DateTime.UtcNow;
            return GetCurrentValue(ref now);
        }

        #region 弃用代码
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="propertyNamePrefix">i设置增量，d设置计算间隔。</param>
        ///// <param name="val"></param>
        ///// <returns></returns>
        //public bool SetPropertyValue(char propertyNamePrefix, object val)
        //{
        //    switch (propertyNamePrefix)
        //    {
        //        case 'i':
        //            if (!OwConvert.TryToDecimal(val, out var dec))
        //            {
        //                return false;
        //            }

        //            StepValue = dec;
        //            break;
        //        case 'd':
        //            if (!OwConvert.TryToDecimal(val, out dec))
        //            {
        //                return false;
        //            }

        //            Delay = TimeSpan.FromSeconds((double)dec);
        //            break;
        //        case 'm':
        //            if (!OwConvert.TryToDecimal(val, out dec))
        //            {
        //                return false;
        //            }

        //            MaxValue = dec;
        //            break;
        //        case 'c':   //当前刷新后的最后值
        //            if (!OwConvert.TryToDecimal(val, out dec))
        //            {
        //                return false;
        //            }

        //            _LastValue = dec;
        //            LastDateTime = DateTime.UtcNow;
        //            break;
        //        case 'l':
        //            if (!OwConvert.TryToDecimal(val, out dec))
        //            {
        //                return false;
        //            }

        //            _LastValue = dec;
        //            break;
        //        case 't':
        //            if (!DateTime.TryParse(val as string, out var dt))
        //            {
        //                return false;
        //            }

        //            LastDateTime = dt;
        //            break;
        //        default:
        //            return false;
        //    }
        //    return true;
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyNamePrefix"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        //public bool TryGetPropertyValue(char propertyNamePrefix, out object result)
        //{
        //    switch (propertyNamePrefix)
        //    {
        //        case 'i':   //增量
        //            result = StepValue;
        //            break;
        //        case 'd':   //增量间隔，单位:秒
        //            result = (decimal)Delay.TotalSeconds;
        //            break;
        //        case 'm':   //最大值
        //            result = MaxValue;
        //            break;
        //        case 'c':   //当前刷新后的最后值
        //            result = GetCurrentValueWithUtc();
        //            break;
        //        case 'l':   //最后计算结果值
        //            result = _LastValue;
        //            break;
        //        case 't':   //最后计算时间点
        //            GetCurrentValueWithUtc();
        //            result = LastDateTime.ToString("s");
        //            break;
        //        default:
        //            result = default;
        //            return false;
        //    }
        //    return true;
        //}

        #endregion 弃用代码

        #region 事件及相关

        /// <summary>
        /// 设置最后计算得到的值，同时将计算时间更新到最接近指定点的时间。
        /// </summary>
        /// <param name="val">这个时间点不晚于指定时间点，且又是正好一跳的时间点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLastValue(decimal val, ref DateTime dateTime)
        {
            var remainder = (dateTime - LastDateTime).Ticks % Delay.Ticks;
            LastDateTime = dateTime.AddTicks(-remainder);
            if (LastDateTime > dateTime)    //若时间点超过指定值
                LastDateTime -= Delay;
            dateTime = LastDateTime;
            _LastValue = val;
        }

        #endregion 事件及相关

        #region IValidatableObject接口相关

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return default;
        }

        #endregion IValidatableObject接口相关
    }

    public static class GameHelper
    {
        /// <summary>
        /// 随机获取指定数量的元素。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="random"></param>
        /// <param name="count">不可大于<paramref name="src"/>中元素数。</param>
        /// <returns></returns>
        public static IEnumerable<T> GetRandom<T>(IEnumerable<T> src, Random random, int count)
        {
            var tmp = src.ToList();
            if (count > tmp.Count)
                throw new InvalidOperationException();
            else if (count == tmp.Count)
                return tmp;
            var result = new T[count];
            for (int i = 0; i < count; i++)
            {
                var index = random.Next(tmp.Count);
                result[i] = tmp[index];
                tmp.RemoveAt(index);
            }
            return result;
        }

        /// <summary>
        /// 获取指定数量的随机整数，且无重复
        /// </summary>
        /// <param name="random"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<int> GetDistinctRandomNumber(Random random, int count, int minValue, int maxValue)
        {
            if (maxValue - minValue < count)
                throw new ArgumentException("指定区间没有足够的数量生成无重复的序列。");
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = random.Next(minValue, maxValue);
            }
            result = result.Distinct().ToArray();
            while (result.Length < count)
            {

            }
            return result;
        }
    }


}