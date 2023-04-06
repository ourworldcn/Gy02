using Gy02.Publisher;
using Gy02Bll.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    public class GameEntityManagerOptions : IOptions<GameEntityManagerOptions>
    {
        public GameEntityManagerOptions Value => this;
    }


    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameEntityManager : GameManagerBase<GameEntityManagerOptions, GameEntityManager>
    {
        public GameEntityManager(IOptions<GameEntityManagerOptions> options, ILogger<GameEntityManager> logger, TemplateManager templateManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;

        /// <summary>
        /// 规范化物品，使之数量符合堆叠上限要求。
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public IEnumerable<GameEntity> Normalize(IEnumerable<GameEntity> src)
        {
            var result = new List<GameEntity>();
            src.SafeForEach(c => _TemplateManager.SetTemplate((VirtualThing)c.Thing));
            foreach (var item in src)
            {
                var tt = _TemplateManager.Id2FullView.GetValueOrDefault(item.TemplateId);
                if (tt is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"物品{item.Id},没有有效模板TId={item.TemplateId}");
                }
                if (tt.Stk == 1)   //若是不可堆叠物
                {
                    var oldCount = item.Count;
                    if (Math.Abs(item.Count) > 1)    //若需要规范化
                    {
                        item.Count = Math.Sign(oldCount);
                        result.Add(item);
                        for (int i = Convert.ToInt32(Math.Abs(item.Count) - 1); i > 0 - 1; i--)
                        {

                        }
                    }
                }
            }
            return result;
        }
    }
}
