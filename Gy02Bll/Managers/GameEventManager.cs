﻿using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{

    public class GameEventManagerOptions : IOptions<GameEventManagerOptions>
    {
        public GameEventManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏内事件服务类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class GameEventManager : GameManagerBase<GameEventManagerOptions, GameEventManager>
    {
        public GameEventManager(IOptions<GameEventManagerOptions> options, ILogger<GameEventManager> logger,
            GameTemplateManager gameTemplateManager, GameEntityManager entityManager, GameAchievementManager achievementManager) : base(options, logger)
        {
            _TemplateManager = gameTemplateManager;
            _EntityManager = entityManager;
            _AchievementManager = achievementManager;

            Initialize();
        }

        void Initialize()
        {
        }

        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;
        GameAchievementManager _AchievementManager;

        ConcurrentDictionary<Guid, TemplateStringFullView> _Id2Template;

        public ConcurrentDictionary<Guid, TemplateStringFullView> Id2Template => LazyInitializer.EnsureInitialized(ref _Id2Template, () =>
        {
            var coll = _TemplateManager.Id2FullView.Where(c => c.Value.GameEvent is GameEventTO);
            return new ConcurrentDictionary<Guid, TemplateStringFullView>(coll);
        });

        ILookup<Guid, TemplateStringFullView> _EventId2Template;

        public ILookup<Guid, TemplateStringFullView> EventId2Template => LazyInitializer.EnsureInitialized(ref _EventId2Template,
            () => Id2Template.ToLookup(c => c.Value.GameEvent.EventId, c => c.Value));

        /// <summary>
        /// 发送游戏内事件。
        /// </summary>
        /// <param name="eventTId">事件的Id。</param>
        /// <param name="count">此次发生的值。</param>
        /// <param name="context">上下文。</param>
        public void SendEvent(Guid eventTId, decimal count, IGameContext context)
        {
            var tts = EventId2Template[eventTId];   //处理模板
            List<GameEntitySummary> entities = new List<GameEntitySummary>();
            foreach (var tt in tts)
            {
                foreach (var entitySummary in tt.GameEvent.Outs)
                {
                    if (_AchievementManager.GetTemplateById(entitySummary.TId) is TemplateStringFullView achi)   //若是成就
                    {
                        _AchievementManager.RaiseEventIfIncreaseAndChanged(entitySummary.TId, achi.Count == 0 ? count : achi.Count, context.GameChar, context.WorldDateTime);
                    }
                    else //若是其它实体
                    {
                        entities.Add(entitySummary);    //暂存需要创建的实体摘要
                    }
                }
            }
            _EntityManager.CreateAndMove(entities, context.GameChar, context.Changes);  //创建实体
        }
    }
}
