using GY02.Templates;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Managers
{
    /// <summary>
    /// 转换器的上下文。
    /// </summary>
    public class EntitySummaryConverterContext
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public EntitySummaryConverterContext()
        {

        }

        /// <summary>
        /// 设置角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 记录变化数据的集合。
        /// </summary>
        public ICollection<GamePropertyChangeItem<object>> Change
        {
            get { return ExtraParams.GetValueOrDefault(nameof(Change)) as ICollection<GamePropertyChangeItem<object>>; }
            set { ExtraParams[nameof(Change)] = value; }
        }

        /// <summary>
        /// 是否忽略保底策略。
        /// </summary>
        public bool IgnoreGuarantees { get => ExtraParams.GetBooleanOrDefaut(nameof(IgnoreGuarantees)) is bool b ? b : false; set => ExtraParams[nameof(IgnoreGuarantees)] = value; }

        /// <summary>
        /// 随机性输出时使用的随机数生成器。
        /// </summary>
        public Random Random { get => ExtraParams.GetValueOrDefault(nameof(Random)) as Random; set => ExtraParams[nameof(Random)] = value; }

        private Dictionary<string, object> _ExtraParams;
        /// <summary>
        /// 设置额外参数，参数由各个转换器自己解释。
        /// </summary>
        public Dictionary<string, object> ExtraParams { get => _ExtraParams ??= new Dictionary<string, object>(); set => _ExtraParams = value; }
    }

    public interface IEntitySummaryConverter
    {
        /// <summary>
        /// 变换实体描述对象。
        /// </summary>
        /// <param name="source">源实体秒对象。</param>
        /// <param name="dest">转换后的结果。</param>
        /// <param name="context">上下文对象。</param>
        /// <param name="changed">是否实际发生了转换。</param>
        /// <returns></returns>
        public bool ConvertEntitySummary(IEnumerable<GameEntitySummary> source, ICollection<(GameEntitySummary, IEnumerable<GameEntitySummary>)> dest, EntitySummaryConverterContext context, out bool changed);
    }

    public abstract class GameManagerBase<TOptions, TService> : OwServiceBase<TOptions, TService> where TOptions : class
    {
        #region 构造函数及相关

        protected GameManagerBase(IOptions<TOptions> options, ILogger<TService> logger) : base(options, logger)
        {
        }

        #endregion 构造函数及相关

        #region IDisposable接口相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable接口相关

    }
}
