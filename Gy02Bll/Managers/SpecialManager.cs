using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Gy02Bll.Managers
{
    public class SpecialManagerOptions : IOptions<SpecialManagerOptions>
    {
        public SpecialManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏特定需求的功能封装管理类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class SpecialManager : GameManagerBase<SpecialManagerOptions, SpecialManager>
    {
        public SpecialManager(IOptions<SpecialManagerOptions> options, ILogger<SpecialManager> logger) : base(options, logger)
        {
        }

        #region 孵化相关

        /// <summary>
        /// 获取孵化的预览信息。
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="gameChar"></param>
        /// <returns>null表示没有找到指定项。</returns>
        public FuhuaSummary GetFuhuaSummary(IEnumerable<string> keys, GameChar gameChar)
        {
            keys = keys.OrderBy(c => c).ToArray();
            return gameChar.FuhuaPreview.FirstOrDefault(c => c.ParentTIds.SequenceEqual(keys));
        }

        public FuhuaSummary GetOrAddFuhuaSummary(IEnumerable<string> keys, GameChar gameChar)
        {
            var result = GetFuhuaSummary(keys, gameChar);
            if (result is null)
            {
                result = new FuhuaSummary { };
                result.ParentTIds.AddRange(keys.OrderBy(c => c));
                gameChar.FuhuaPreview.Add(result);
            }
            return result;
        }

        /// <summary>
        /// 获取孵化的历史信息。
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="gameChar"></param>
        /// <returns>null表示没有找到指定项。</returns>
        public FuhuaSummary GetFuhuaHistory(IEnumerable<string> keys, GameChar gameChar)
        {
            keys = keys.OrderBy(c => c).ToArray();
            return gameChar.FuhuaHistory.FirstOrDefault(c => c.ParentTIds.SequenceEqual(keys));
        }

        public FuhuaSummary GetOrAddHistory(IEnumerable<string> keys, GameChar gameChar)
        {
            var result = GetFuhuaHistory(keys, gameChar);
            if (result is null)
            {
                result = new FuhuaSummary { };
                result.ParentTIds.AddRange(keys.OrderBy(c => c));
                gameChar.FuhuaHistory.Add(result);
            }
            return result;
        }
        #endregion 孵化相关
    }
}
