using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 简单动态扩展属性（Simple dynamic extension properties，Sdep）接口。
    /// </summary>
    /// <typeparam name="T">动态属性值的类型。</typeparam>
    public interface ISimpleDynamicProperty<T>
    {
        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该属性。
        /// 频繁发生变化的战斗属性，请另行生成对象。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public abstract Dictionary<string, T> Properties
        {
            get;
        }

        /// <summary>
        /// 追加或设置动态属性。
        /// 虽然一般动态属性存在于<see cref="Properties"/>中，但派生类可能需要存储在其它位置，可重载此成员以控制读写位置。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void SetSdp(string name, T value);

        /// <summary>
        /// 获取动态属性。
        /// 虽然一般动态属性存在于<see cref="Properties"/>中，但派生类可能需要存储在其它位置，可重载此成员以控制读写位置。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>true找到属性并返回，false没有找到指定名称的属性。</returns>
        public abstract bool TryGetSdp(string name, out T value);

        /// <summary>
        /// 移除动态属性。
        /// </summary>
        /// <param name="name"></param>
        /// <returns>true成功移除，false指定属性不存在或是不可移除的属性。</returns>
        public abstract bool RemoveSdp(string name);

        /// <summary>
        /// 获取所有动态属性。
        /// </summary>
        /// <returns>注意在遍历返回集合时一般不可以更改集合，除非实现类有特别说明。</returns>
        public IEnumerable<(string, T)> GetAllSdp();
    }

}