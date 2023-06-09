using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class SequenceOutManagerOptions : IOptions<SequenceOutManagerOptions>
    {
        public SequenceOutManagerOptions()
        {

        }

        public SequenceOutManagerOptions Value => this;
    }

    /// <summary>
    /// 动态输出的管理器。
    /// </summary>
    public class SequenceOutManager : GameManagerBase<SequenceOutManagerOptions, SequenceOutManager>
    {
        public SequenceOutManager(IOptions<SequenceOutManagerOptions> options, ILogger<SequenceOutManager> logger, GameEntityManager entityManager, BlueprintManager blueprintManager) : base(options, logger)
        {
            _EntityManager = entityManager;
            _BlueprintManager = blueprintManager;
        }

        GameEntityManager _EntityManager;

        BlueprintManager _BlueprintManager;

        #region 获取信息相关

        public SequenceOut GetMultOutByTemplate(TemplateStringFullView template)
        {
            var result = template.SequenceOut;
            if (result is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板没有动态输出的定义，TId={template.TemplateId}");
            }
            return result;
        }


        #endregion 获取信息相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="summaries"></param>
        /// <param name="result"></param>
        public void GetOuts(GameChar gameChar, IEnumerable<GameEntitySummary> summaries, List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> result)
        {
            
        }

        /// <summary>
        /// 获取动态输出的实体摘要。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool GetOut(GameChar gameChar, TemplateStringFullView tt, out GameEntitySummary result)
        {
            var baseColl = _EntityManager.GetAllEntity(gameChar).Append(gameChar);  //获取所有的实体
            return GetOut(baseColl, tt, out result);
        }

        /// <summary>
        /// 获取动态输出的实体摘要。
        /// </summary>
        /// <param name="entities">仅在此集合内寻找匹配的实体。这个集合内实体是获取索引必须的，而不与产出相关。</param>
        /// <param name="tt"></param>
        /// <param name="result">产出。</param>
        /// <returns>true成功获取到了实体摘要，false失败，此时调用<see cref="OwHelper.GetLastError(out string)"/>可获取详细信息。</returns>
        public bool GetOut(IEnumerable<GameEntity> entities, TemplateStringFullView tt, out GameEntitySummary result)
        {
            var mo = GetMultOutByTemplate(tt);
            if (mo is null) goto lbErr;


            var coll = entities.Where(c => _EntityManager.IsMatch(c, mo.Conditions));    //符合条件的实体集合
            var entity = coll.FirstOrDefault();
            if (entity is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到适合条件的实体, TId = {tt.TemplateId}");
                goto lbErr;
            }
            if (!mo.GetIndexExpression.TryGetValue(entity, out var indexObj)) goto lbErr;
            var index = Convert.ToInt32(indexObj);
            result = mo.Outs.GetItem(index);
            return true;
        lbErr:
            result = null;
            return false;
        }
    }

}

