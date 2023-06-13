using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
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
    public class GameDiceManager : GameManagerBase<GameDiceManagerOptions, GameDiceManager>, IEntitySummaryConverter
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

        /// <summary>
        /// 用随机数获取指定的池项中的随机一项。
        /// </summary>
        /// <param name="items"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public GameDiceItem Roll(IEnumerable<GameDiceItem> items, Random random = null)
        {
            var totalWeight = items.Sum(c => c.Weight);    //总权重
            random ??= new Random();
            var total = (decimal)random.NextDouble() * totalWeight;
            foreach (var item in items)
            {
                if (item.Weight <= decimal.Zero) continue; //容错
                if (total <= item.Weight) return item;
                total -= item.Weight;
            }
            return default;
        }

        /// <summary>
        /// 在指定项中获取指定次数的项。
        /// </summary>
        /// <param name="items"></param>
        /// <param name="maxCount">至多生成这么多项。可能实际生成的项，少于指定的项。返回时，是实际roll的次数，仅当不允许重复项时可能出现全部物品已经roll到，但次数未达到的情况。</param>
        /// <param name="allowRepetition">true要求允许多次获取相同的结果。</param>
        /// <param name="random"></param>
        /// <returns></returns>
        public List<GameDiceItem> Roll(IEnumerable<GameDiceItem> items, ref int maxCount, bool allowRepetition, Random random = null)
        {
            HashSet<GameDiceItem> hs = new HashSet<GameDiceItem>(items);
            random ??= new Random();
            List<GameDiceItem> result = new List<GameDiceItem>();
            GameDiceItem tmp;
            int i;
            for (i = 1; i <= maxCount; i++)
            {
                tmp = Roll(hs, random);
                if (tmp is null) return null;
                result.Add(tmp);
                if (!allowRepetition)
                {
                    hs.Remove(tmp);
                    if (hs.Count <= 0) break;   //若已没有项
                }
            }
            maxCount = Math.Min(maxCount, i);
            return result;

        }

        /// <summary>
        /// roll卡池。
        /// </summary>
        /// <param name="diceTT">卡池</param>
        /// <param name="gameChar"></param>
        /// <param name="ignoreGuarantees">是否忽略保底计数，true忽略（不会记录保底次数的变化），false,不忽略。</param>
        /// <param name="random"></param>
        /// <returns>命中的项，注意这些项可能是保底卡池中的项。</returns>
        public List<GameDiceItem> Roll(TemplateStringFullView diceTT, GameChar gameChar, bool ignoreGuarantees = false, Random random = null)
        {
            random ??= new Random();
            var maxGuaranteesCount = GetGuaranteesCount(diceTT);    //最大保底数
            if (ignoreGuarantees || maxGuaranteesCount is null) //若忽略保底问题
            {
                var count = diceTT.Dice.MaxCount;
                return Roll(diceTT.Dice.Items, ref count, diceTT.Dice.AllowRepetition, random);
            }
            else //若考虑保底问题
            {
                var history = GetOrAddHistory(diceTT, gameChar);    //历史数据项
                if (history.GuaranteesCount >= maxGuaranteesCount - 1)  //若需要保底
                {
                    var count = diceTT.Dice.MaxCount;
                    var result = Roll(diceTT.Dice.Items, ref count, diceTT.Dice.AllowRepetition, random);
                    history.GuaranteesCount = 0;    //清理保底计数
                    if (result.Any(c => c.ClearGuaranteesCount))   //若出了保底项
                        return result;
                    //用保底池再次roll
                    var dice = GetGuaranteesDice(diceTT);
                    count = dice.Dice.MaxCount;
                    return Roll(dice.Dice.Items, ref count, dice.Dice.AllowRepetition, random);
                }
                else //若无需保底
                {
                    var count = diceTT.Dice.MaxCount;
                    var result = Roll(diceTT.Dice.Items, ref count, diceTT.Dice.AllowRepetition, random);
                    if (result is not null) history.GuaranteesCount++;  //增加保底计数
                    return result;
                }
            }

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
                    var coll = _TemplateManager.Id2FullView.Where(c => c.Value?.DiceGroup is not null);
                    foreach (var item in coll)
                    {
                        item.Value.DiceGroup.DiceIds.ForEach(c => tmp[c] = item.Value);
                    }

                    Interlocked.CompareExchange(ref _DiceTId2DiceGroup, tmp, null);
                }
                return _DiceTId2DiceGroup;
            }
        }

        /// <summary>
        /// 用卡池的Id获取所属的卡池组对象。
        /// </summary>
        /// <param name="diceTid"></param>
        /// <returns>卡池组对象，若没有找到则返回null。</returns>
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

        /// <summary>
        /// 获取指定池子的历史记录项，如果卡池属于卡池组则找卡池组的记录项，若尚未有记录项则自动加入。
        /// </summary>
        /// <param name="dice"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public GameDiceHistoryItem GetOrAddHistory(TemplateStringFullView dice, GameChar gameChar)
        {
            GameDiceHistoryItem result = null;
            var group = GetDiceGroupByDiceTId(dice.TemplateId);
            if (group is null)   //若找不到所属组
            {
                result = gameChar.DiceHistory.FirstOrDefault(c => c.DiceTId == dice.TemplateId);
                if (result is null) //若需要初始化
                {
                    result = new GameDiceHistoryItem
                    {
                        DiceTId = dice.TemplateId,
                        GuaranteesCount = 0,
                    };
                    gameChar.DiceHistory.Add(result);
                }
            }
            else //若找到了所属组
            {
                result = gameChar.DiceHistory.FirstOrDefault(c => c.DiceTId == group.TemplateId);
                if (result is null) //若需要初始化
                {
                    result = new GameDiceHistoryItem
                    {
                        DiceTId = group.TemplateId,
                        GuaranteesCount = 0,
                    };
                }
                gameChar.DiceHistory.Add(result);
            }
            return result;
        }

        /// <summary>
        /// 获取最大保底数。
        /// </summary>
        /// <param name="dice"></param>
        /// <returns>保底次数，null表示不保底</returns>
        public int? GetGuaranteesCount(TemplateStringFullView dice)
        {
            var group = GetDiceGroupByDiceTId(dice.TemplateId);
            if (group is null)   //若找不到所属组
                return dice.Dice.GuaranteesCount;
            else //若找到了所属组
                return group.DiceGroup.GuaranteesCount;
        }

        /// <summary>
        /// 获取保底专用卡池。
        /// </summary>
        /// <param name="dice"></param>
        /// <returns></returns>
        public TemplateStringFullView GetGuaranteesDice(TemplateStringFullView dice)
        {
            var group = GetDiceGroupByDiceTId(dice.TemplateId);
            if (group is null)   //若找不到所属组
                if (dice.Dice.GuaranteesDiceTId is null)
                    return null;
                else
                    return GetDiceById(dice.Dice.GuaranteesDiceTId.Value);
            else //若找到了所属组
                return GetDiceById(group.DiceGroup.GuaranteesDiceTId);

        }

        #endregion 卡池组相关

        #region 计算卡池相关

        /// <summary>
        /// 获取卡池项的输出项。
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public IEnumerable<(GameDiceItem, IEnumerable<GameEntitySummary>)> GetOutputs(IEnumerable<GameDiceItem> items)
        {
            var result = from tmp in items
                         select (tmp, tmp.Outs.Select(c => (GameEntitySummary)c.Clone()));
            return result;
        }

        /// <summary>
        /// 在输出项中如果有指向卡池的项，则会自动使用卡池roll并返回相应项。
        /// </summary>
        /// <param name="outItem"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public List<GameEntitySummary> Transformed(GameEntitySummary outItem, GameChar gameChar, bool ignoreGuarantees = false, Random random = null)
        {
            var dice = GetDiceById(outItem.TId);
            if (dice is null)
                return new List<GameEntitySummary> { outItem };
            random ??= new Random();
            var items = Roll(dice, gameChar, ignoreGuarantees, random);
            var result = items.SelectMany(c => c.Outs).ToList();
            return result;
        }

        #endregion 计算卡池相关

        #region IEntitySummaryConverter接口及相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="source">要转换的实体集合</param>
        /// <param name="dest"></param>
        /// <param name="context">上下文。</param>
        /// <param name="changed">是否发生了至少一个转换。</param>
        /// <returns><inheritdoc/></returns>
        public bool ConvertEntitySummary(IEnumerable<GameEntitySummary> source, ICollection<(GameEntitySummary, IEnumerable<GameEntitySummary>)> dest, EntitySummaryConverterContext context, out bool changed)
        {
            changed = false;
            List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)>();
            foreach (var summary in source)
            {
                var tmp = Transformed(summary, context.GameChar, context.IgnoreGuarantees, context.Random);
                if (tmp is null) goto lbErr;
                if (tmp.Count != 1 || tmp[0] != summary) changed = true;
                list.Add((summary, tmp));
            }
            list.ForEach(c => dest.Add(c));
            return true;
        lbErr:
            changed = false;
            return false;
        }

        #endregion IEntitySummaryConverter接口及相关

    }

}
