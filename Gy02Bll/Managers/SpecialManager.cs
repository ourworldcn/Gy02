﻿using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using System;

namespace GY02.Managers
{
    public class SpecialManagerOptions : IOptions<SpecialManagerOptions>
    {
        public SpecialManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏特定需求的功能封装管理类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class SpecialManager : GameManagerBase<SpecialManagerOptions, SpecialManager>, IEntitySummaryConverter
    {
        public SpecialManager(IOptions<SpecialManagerOptions> options, ILogger<SpecialManager> logger, GameTemplateManager templateManager, GameDiceManager diceManager, GameSequenceManager sequenceOutManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _DiceManager = diceManager;
            _SequenceOutManager = sequenceOutManager;
        }

        GameTemplateManager _TemplateManager;
        GameDiceManager _DiceManager;
        GameSequenceManager _SequenceOutManager;

        #region 孵化相关

        /// <summary>
        /// 获取指定孵化的产出预览项。考虑了排除规则。
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public List<GameDiceItem> GetFuhuaEntitySummary(IEnumerable<string> keys, GameChar gameChar)
        {
            List<GameDiceItem> result = new List<GameDiceItem>();
            var history = GetOrAddFuhuaHistory(keys, gameChar);
            var info = GetFuhuaInfo(keys);
            var mounts = _DiceManager.Roll(info.Item2, gameChar, true);
            //获取皮肤槽选项
            var dicePifu = (GameDice)info.Item3.Dice.Clone();
            var hs = new HashSet<Guid>(history.Items.SelectMany(d1 => d1.Outs.Select(d => d.TId)));
            dicePifu.Items.RemoveAll(c => hs.Overlaps(c.Outs.Select(d => d.TId)));
            var maxCount = dicePifu.MaxCount;

            var pifus = dicePifu.Items.Count > 0 ? _DiceManager.Roll(dicePifu.Items, ref maxCount, dicePifu.AllowRepetition) : new List<GameDiceItem>();

            result.AddRange(mounts);
            result.AddRange(pifus);
            return result;
        }

        /// <summary>
        /// 获取孵化信息中双亲的类属信息。
        /// </summary>
        /// <param name="fuhua"></param>
        /// <returns>返回值已经排序。</returns>
        public static string[] GetFuhuaKey(FuhuaInfo fuhua)
        {
            var result = new string[]
            {
               fuhua.Parent1Conditional.First().Genus[0],   //孵化信息必须是这个结构
               fuhua.Parent2Conditional.First().Genus[0],
            };
            Array.Sort(result);
            return result;
        }

        /// <summary>
        /// 返回孵化的模板信息。
        /// </summary>
        /// <param name="parentGenus"></param>
        /// <returns>(孵化模板，动物模板，皮肤模板)</returns>
        public (TemplateStringFullView, TemplateStringFullView, TemplateStringFullView) GetFuhuaInfo(IEnumerable<string> parentGenus)
        {
            var fuhua = _TemplateManager.Id2FullView.Values.Where(c => c.Fuhua is not null).FirstOrDefault(c => GetFuhuaKey(c.Fuhua).SequenceEqual(parentGenus));
            if (fuhua is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定类属组合的孵化信息{parentGenus}");
                return (null, null, null);
            }
            var mounts = _TemplateManager.GetFullViewFromId(fuhua.Fuhua.DiceTId1);
            if (mounts is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定的卡池模板，TId={fuhua.Fuhua.DiceTId1}");
                return (null, null, null);
            }
            var pifus = _TemplateManager.GetFullViewFromId(fuhua.Fuhua.DiceTId2);
            if (pifus is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定的卡池模板，TId={fuhua.Fuhua.DiceTId1}");
                return (null, null, null);
            }
            return (fuhua, mounts, pifus);
        }

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

        public FuhuaSummary GetOrAddFuhuaHistory(IEnumerable<string> keys, GameChar gameChar)
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

        /// <summary>
        /// 指定的实体预览内需要翻译的骰子和序列翻译为最终输出的实体。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="context"></param>
        /// <returns>true成功翻译，false出现错误。</returns>
        public bool Transformed(IEnumerable<GameEntitySummary> source, ICollection<(GameEntitySummary, IEnumerable<GameEntitySummary>)> dest, EntitySummaryConverterContext context)
        {
            IEntitySummaryConverter[] svcs = new IEntitySummaryConverter[] { _DiceManager, _SequenceOutManager, this };

            IEnumerable<GameEntitySummary> tmpSource = source;
            List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> tmpDest = null;
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var svc in svcs)
                {

                    tmpDest = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)>();
                    if (!svc.ConvertEntitySummary(tmpSource, tmpDest, context, out var changedTmp)) goto lbErr;  //若失败
                    changed = changed || changedTmp;
                    tmpSource = tmpDest.SelectMany(c => c.Item2);
                }
            }
            tmpDest?.ForEach(c => dest.Add(c));
            return true;
        lbErr:  //出错
            changed = false;
            return false;
        }

        /// <summary>
        /// 翻译指定的商品模板产出。
        /// </summary>
        /// <param name="tt"></param>
        /// <param name="entitySummaries"></param>
        /// <param name="gc"></param>
        /// <returns></returns>
        public bool Transformed(TemplateStringFullView tt, ICollection<(GameEntitySummary, IEnumerable<GameEntitySummary>)> entitySummaries, GameChar gc)
        {
            var tmp = tt?.ShoppingItem;
            if (tmp is null)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"指定模板没有商品输出项，TId={tt.TemplateId}");
                return false;
            }
            var outs = tmp?.Outs;
            if (outs is null || outs.Count == 0) return true; //若没有有产出项
            var list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> { };
            var b = Transformed(outs, list, new EntitySummaryConverterContext
            {
                Change = null,
                GameChar = gc,
                IgnoreGuarantees = false,
                Random = new Random(),
            });
            if (!b) return false;
            list.ForEach(c => entitySummaries.Add(c));
            return true;
        }

        /// <summary>
        /// 转化特定的物品占位符，如结算后看广告的产出占位符。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="context"></param>
        /// <param name="changed"></param>
        /// <returns></returns>
        public bool ConvertEntitySummary(IEnumerable<GameEntitySummary> source, ICollection<(GameEntitySummary, IEnumerable<GameEntitySummary>)> dest, EntitySummaryConverterContext context, out bool changed)
        {
            changed = false;
            List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)>();
            foreach (var summary in source)
            {
                if (summary.TId == ProjectContent.AdsCombatTid)    //若是广告购买物品
                {
                    changed = true;
                    list.Add((summary, context.GameChar.AdsRewardsHistory));
                }
                else
                {
                    list.Add((summary, new GameEntitySummary[] { summary }));
                }
            }
            list.ForEach(c => dest.Add(c));
            return true;
        }
    }
}
