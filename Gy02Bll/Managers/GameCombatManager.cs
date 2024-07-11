using AutoMapper;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Manager;
using OW.Game.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    /// <summary>
    /// 战斗管理器的配置选项。
    /// </summary>
    [AutoMap(typeof(Dictionary<string, object>))]
    public class GameCombatManagerOptions : IOptions<GameCombatManagerOptions>
    {
        public GameCombatManagerOptions()
        {

        }

        public GameCombatManagerOptions Value => this;

        /// <summary>
        /// 打塔往上排名数量。
        /// </summary>
        public int TowerUpCount { get; set; }

        /// <summary>
        /// 打塔往下排名数量（含本层）。
        /// </summary>
        public int TowerDownCount { get; set; }

        /// <summary>
        /// 上手取值范围权重。
        /// </summary>
        public int HardWeight { get; set; }

        /// <summary>
        /// 平手取值范围权重。
        /// </summary>
        public int NormalWeight { get; set; }

        /// <summary>
        /// 下手取值范围权重。
        /// </summary>
        public int EasyWeight { get; set; }
    }

    /// <summary>
    /// 战斗管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class GameCombatManager : GameManagerBase<GameCombatManagerOptions, GameCombatManager>
    {
        public GameCombatManager(IOptions<GameCombatManagerOptions> options, ILogger<GameCombatManager> logger, GameTemplateManager templateManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            Initialize();
        }

        private void Initialize()
        {
            var tt = _TemplateManager.GetFullViewFromId(Guid.Parse("524143AA-0E67-4E9B-8FBF-9BB8AFB586EA"));
            if (tt is null)
            {
                Options.TowerUpCount = 30;
                Logger.LogWarning("未能找到爬塔配置模板TId={tid}", Guid.Parse("524143AA-0E67-4E9B-8FBF-9BB8AFB586EA"));
            }
            else
            {
                if (OwConvert.TryToDecimal(tt.ExtraProperties.GetValueOrDefault("HardWeight"), out var hardWeight))
                    Options.HardWeight = (int)hardWeight;
                if (OwConvert.TryToDecimal(tt.ExtraProperties.GetValueOrDefault("NormalWeight"), out var normalWeight))
                    Options.NormalWeight = (int)normalWeight;
                if (OwConvert.TryToDecimal(tt.ExtraProperties.GetValueOrDefault("EasyWeight"), out var easyWeight))
                    Options.EasyWeight = (int)easyWeight;
                if (OwConvert.TryToDecimal(tt.ExtraProperties.GetValueOrDefault("TowerUpCount"), out var towerUpCount))
                    Options.TowerUpCount = (int)towerUpCount;
                if (OwConvert.TryToDecimal(tt.ExtraProperties.GetValueOrDefault("TowerDownCount"), out var towerDownCount))
                    Options.TowerDownCount = (int)towerDownCount;
            }

        }

        GameTemplateManager _TemplateManager;


        List<TemplateStringFullView> _Towers;
        /// <summary>
        /// 塔层数据。按层数升序排列。
        /// </summary>
        List<TemplateStringFullView> Towers
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _Towers, () =>
                {
                    return default;
                });
            }
        }

        public TemplateStringFullView GetTemplateById(Guid tId)
        {
            var tt = _TemplateManager.GetFullViewFromId(tId);
            return tt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentId"></param>
        /// <returns>(下手Id，平手Id，上手Id)</returns>
        public (Guid, Guid, Guid) GetNewLevel(Guid currentId)
        {
            Random rnd = new Random();
            var index = Towers.FindIndex(c => c.TemplateId == currentId); //当前塔层索引

            var maxIndex = Math.Min(index + Options.TowerUpCount, Towers.Count - 1); //最大索引
            var minIndex = Math.Max(index - Options.TowerDownCount + 1, 0); //最小索引

            var total = Options.HardWeight + Options.NormalWeight + Options.EasyWeight; //总权重
            var totalCount = (maxIndex - minIndex + 1); //总数
            var hardCount = (int)Math.Round((decimal)Options.HardWeight / total * totalCount);   //上手池数量
            var normalCount = (int)Math.Round((decimal)Options.NormalWeight / total * totalCount);   //平手池数量
            var easyCount = totalCount - hardCount - normalCount;   //下手池数量
            //取随即索引
            var hardIndex = rnd.Next(hardCount);    //上手索引偏移
            var normalIndex = rnd.Next(normalCount);    //平手索引偏移
            var easyIndex = rnd.Next(easyCount);    //下手索引偏移

            return (Towers[easyIndex].TemplateId, Towers[easyCount + normalIndex].TemplateId, Towers[easyCount + normalCount + hardIndex].TemplateId);
        }
    }
}
