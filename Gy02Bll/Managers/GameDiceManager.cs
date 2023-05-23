using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    /// <summary>
    /// 卡池管理器的配置类。
    /// </summary>
    public class GameDiceManagerOptions : IOptions<GameDiceManagerOptions>
    {
        public GameDiceManagerOptions()
        {

        }

        public GameDiceManagerOptions Value => this;
    }

    /// <summary>
    /// 卡池相关功能。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameDiceManager : GameManagerBase<GameDiceManagerOptions, GameDiceManager>
    {
        public GameDiceManager(IOptions<GameDiceManagerOptions> options, ILogger<GameDiceManager> logger, TemplateManager templateManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;

        #region 卡池相关

        ConcurrentDictionary<Guid, TemplateStringFullView> _Id2Dice;

        /// <summary>
        /// 获取所有卡池的字典。
        /// </summary>
        public ConcurrentDictionary<Guid, TemplateStringFullView> Id2TemplateStringFullView
        {
            get
            {
                if (_Id2Dice is null)
                {
                    var tmp = new ConcurrentDictionary<Guid, TemplateStringFullView>(_TemplateManager.Id2FullView.Where(c => c.Value.Dice is not null).ToDictionary(c => c.Key, c => c.Value));
                    Interlocked.CompareExchange(ref _Id2Dice, tmp, null);
                }
                return _Id2Dice;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tid"></param>
        /// <returns>卡池的模板，如果不是卡池模板则返回null。</returns>
        public TemplateStringFullView GetDiceById(Guid tid)
        {
            var result = Id2TemplateStringFullView.GetValueOrDefault(tid);
            if (result is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定Id的卡池，TId={tid}");
                return null;
            }
            return result;
        }
        #endregion 卡池相关

        #region 卡池组相关

        ConcurrentDictionary<Guid, TemplateStringFullView> _DiceTId2DiceGroup;

        /// <summary>
        /// 获取卡池对应的卡池组对象。(卡池TId，对应的卡池组对象)
        /// </summary>
        public ConcurrentDictionary<Guid, TemplateStringFullView> DiceTId2DiceGroup
        {
            get
            {
                if (_DiceTId2DiceGroup is null)
                {
                    var tmp = new ConcurrentDictionary<Guid, TemplateStringFullView>();
                    var coll = _TemplateManager.Id2FullView.Where(c => c.Value.DiceGroup is not null);
                    foreach (var item in coll)
                    {
                        item.Value.DiceGroup.DiceIds.ForEach(c => tmp[c] = item.Value);
                    }

                    Interlocked.CompareExchange(ref _DiceTId2DiceGroup, tmp, null);
                }
                return _Id2Dice;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="diceTid"></param>
        /// <returns></returns>
        public TemplateStringFullView GetDiceGroupByDiceTId(Guid diceTid)
        {
            var result = DiceTId2DiceGroup.GetValueOrDefault(diceTid);
            if (result is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定Id的卡池对应的卡池组，卡池TId={diceTid}");
            }
            return result;
        }
        #endregion 卡池组相关

    }
}
