/* **********************************************************************
 * 文件放置游戏专用的一些基础类
 * 一些游戏中常用的基础数据结构。
 ********************************************************************** */
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace OW.Game
{

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