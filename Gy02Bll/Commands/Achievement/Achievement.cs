﻿using GY02.Managers;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * 包含各种未分类成就。
 */

namespace GY02.Commands
{
    /// <summary>
    /// 计算合成次数。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<CompositeCommand>))]
    public class AchievementCompositedHandler : ISyncCommandHandled<CompositeCommand>
    {
        public AchievementCompositedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(CompositeCommand command, Exception exception)
        {
            if (command.HasError || exception is not null) return;
            if (!_AchievementManager.RaiseEventIfLevelChanged(Guid.Parse("4f6a92f2-3aac-479b-92e8-0dfffa74536b"), 1, command.GameChar, OwHelper.WorldNow))
                command.FillErrorFromWorld();
        }
    }

    /// <summary>
    /// 累计孵化次数。
    /// </summary>
    public class AchievementFuhuaedHandler : ISyncCommandHandled<FuhuaCommand>
    {
        public AchievementFuhuaedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(FuhuaCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            if (!_AchievementManager.RaiseEventIfLevelChanged(Guid.Parse("22f48e2b-8e81-43fb-80dd-af900eb21a29"), 1, command.GameChar, OwHelper.WorldNow))
                command.FillErrorFromWorld();
        }
    }

    /// <summary>
    /// 4963c720-2b8f-4def-aed2-7bc8925f6a91	开宝箱次数 gs_choujiangbaoxiang
    /// </summary>
    public class ChoujiangBaoxiangClass : ISyncCommandHandled<ShoppingBuyCommand>
    {

        public ChoujiangBaoxiangClass(GameAchievementManager achievementManager, GameShoppingManager shoppingManager)
        {
            _AchievementManager = achievementManager;
            _ShoppingManager = shoppingManager;
        }

        GameAchievementManager _AchievementManager;
        GameShoppingManager _ShoppingManager;

        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            if (_ShoppingManager.GetShoppingTemplateByTId(command.ShoppingItemTId) is not TemplateStringFullView tt) return;
            if (tt.Genus.Contains("gs_choujiangbaoxiang"))
                if (!_AchievementManager.RaiseEventIfLevelChanged(Guid.Parse("4963c720-2b8f-4def-aed2-7bc8925f6a91"), 1, command.GameChar, OwHelper.WorldNow))
                    command.FillErrorFromWorld();
        }
    }

    /// <summary>
    /// 2d023b02-fb74-4320-9ee4-b6c761938fbe	全部镶嵌装备的等级成就 "Genus":["gs_equipslot"],
    /// </summary>
    public class MyClass : ISyncCommandHandled<MoveItemsCommand>
    {
        public MyClass(GameAchievementManager achievementManager, GameTemplateManager templateManager, GameEntityManager entityManager)
        {
            _AchievementManager = achievementManager;
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
        }

        GameAchievementManager _AchievementManager;
        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;

        public void Handled(MoveItemsCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            if (_TemplateManager.GetFullViewFromId(command.ContainerId) is not TemplateStringFullView tt) return;
            if (tt.Genus.Contains("gs_equipslot"))
            {
                var achiTId = Guid.Parse("2d023b02-fb74-4320-9ee4-b6c761938fbe");
                if (_AchievementManager.GetTemplateById(achiTId) is not TemplateStringFullView achiTt) return;
                if (_AchievementManager.GetOrCreate(command.GameChar, achiTt) is not GameAchievement achi) return;
                var tts = _TemplateManager.Id2FullView.Where(c => c.Value.Genus?.Contains("gs_equipslot") ?? false).Select(c => c.Value);   //所有装备槽
                var ttIds = tts.Select(c => c.TemplateId).ToArray();
                var gc = command.GameChar;
                var things = gc.GetAllChildren().Where(c => c.Parent is not null && ttIds.Contains(c.Parent.ExtraGuid));
                var entitis = things.Select(c => _EntityManager.GetEntity(c));
                var nv = entitis.Sum(c => c.Level); //新的总等级
                var inc = nv - achi.Count;  //等级差
                if (inc > 0)
                    if (!_AchievementManager.RaiseEventIfLevelChanged(Guid.Parse("2d023b02-fb74-4320-9ee4-b6c761938fbe"), inc, command.GameChar, OwHelper.WorldNow))
                        command.FillErrorFromWorld();
            }
        }
    }
}
