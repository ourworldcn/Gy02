using GY02.Templates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
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

        private Dictionary<string, object> _ExtraParams;
        /// <summary>
        /// 设置额外参数，参数由各个转换器自己解释。
        /// </summary>
        public Dictionary<string, object> ExtraParams { get => _ExtraParams ??= new Dictionary<string, object>(); set => _ExtraParams = value; }
    }

    public interface IEntitySummaryConverter
    {
        public bool Convert(IEnumerable<GameEntitySummary> source, ICollection<(GameEntitySummary, IEnumerable<GameEntitySummary>)> dest, EntitySummaryConverterContext context, out bool changed);
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
