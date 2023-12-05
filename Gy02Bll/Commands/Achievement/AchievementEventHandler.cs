using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Commands.Achievement
{
    /// <summary>
    /// 推进成就/任务系统计数的拦截器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class AchievementEventHandler
    {
        public AchievementEventHandler(GameEntityManager entityManager, GameAchievementManager achievementManager, GameBlueprintManager blueprintManager, GameTemplateManager templateManager, GameEventManager eventManager)
        {
            _EntityManager = entityManager;
            _AchievementManager = achievementManager;
            _BlueprintManager = blueprintManager;
            _TemplateManager = templateManager;
            _EventManager = eventManager;

            Initialize();
        }

        private void Initialize()
        {
            _EntityManager.EntityChanged += _EntityManager_EntityChanged;
        }

        GameEntityManager _EntityManager;
        GameAchievementManager _AchievementManager;
        GameBlueprintManager _BlueprintManager;
        GameTemplateManager _TemplateManager;
        GameEventManager _EventManager;

        /// <summary>
        /// 所有装备的类属集合。
        /// </summary>
        static HashSet<string> _Equ = new HashSet<string>() { "e_wuqi", "e_yifu", "e_yaodai", "e_shoutao", "e_xiezi", "e_zuoqi" };

        /// <summary>
        /// 
        /// </summary>
        static Guid GuanggaoEventTId = new Guid("f607ef05-801d-4a48-9f44-0673e55d2bf3");
        private void _EntityManager_EntityChanged(object sender, EntityChangedEventArgs e)
        {
            var now = OwHelper.WorldNow;
            GameChar gc = null;
            foreach (var entity in e.Entities)
            {
                var gcThing = entity.GetThing()?.GetAncestor(c => (c as IDbQuickFind)?.ExtraGuid == ProjectContent.CharTId) as VirtualThing;   //获取用户的宿主对象
                if (gcThing is null) continue;
                //var gc = _EntityManager.GetEntity(gcThing) as GameChar; //获取角色对象
                if (_EntityManager.GetEntity(gcThing) is GameChar gameChar)
                {
                    gc = gameChar;
                    break;
                }
            }
            if (gc is null) return;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, gc, now, null);
            if (e.Entities.Any(c => c.TemplateId == ProjectContent.GuanggaoCurrenyTId && c.Count > 0))  //看广告事件
            {
                _EventManager.SendEvent(GuanggaoEventTId, 1, context);
            }
            foreach (var entity in e.Entities)
            {
                var tt = _EntityManager.GetTemplate(entity);    //获取模板
                if (tt is null) continue;

                if (tt.TemplateId == ProjectContent.LoginedDayTId)    //累计登录天数
                {
                    var achiTt = _AchievementManager.GetTemplateById(Guid.Parse("0d8071a9-34a0-44fc-8264-c5827f95f1e6")); //登录天数成就
                    if (achiTt is null) continue;
                    var achi = _AchievementManager.GetOrCreate(gc, achiTt);
                    if (!_AchievementManager.IsValid(achi, gc, now)) continue;    //若处于无效状态
                    var oldLv = achi.Level;
                    achi.Count++;   //计算计数
                    if (!_AchievementManager.RefreshState(achi, gc, now)) continue;
                    if (achi.Level > oldLv)    //若发生了变化
                    {
                        _AchievementManager.InvokeAchievementChanged(new AchievementChangedEventArgs { Achievement = achi });
                    }
                }
                //拥有装备的数量变化事件（含武器和坐骑）401d63c4-0b2a-42e2-8828-f6d0c80582a2
                if (tt.Genus is not null && _Equ.Overlaps(tt.Genus))    //装备拥有数量成就
                {
                    {
                        var achiTt = _AchievementManager.GetTemplateById(Guid.Parse("d464491f-f7a9-4ae3-b078-a9cc657abd82")); //装备拥有数量成就
                        if (achiTt is null) continue;
                        var achi = _AchievementManager.GetOrCreate(gc, achiTt);
                        if (!_AchievementManager.IsValid(achi, gc, now)) continue;    //若处于无效状态
                        var oldLv = achi.Level;
                        //计算计数
                        var nv = _EntityManager.GetAllEntity(gc).Count(c =>
                        {
                            var genus = _EntityManager.GetTemplate(c)?.Genus;
                            return genus is null ? false : _Equ.Overlaps(genus);
                        });
                        if (achi.Count < nv)   //若装备总数增加了
                        {
                            _EventManager.SendEventWithNewValue(Guid.Parse("401d63c4-0b2a-42e2-8828-f6d0c80582a2"), nv, context);
                        }
                    }
                    //0f83e776-7c47-4fc6-b27b-c5f14f1b158f	拥有武器的数量成就
                    if (tt.Genus.Contains("e_wuqi"))   //拥有武器的数量成就
                    {
                        var nv = _EntityManager.GetAllEntity(gc).Select(c => (c, _TemplateManager.GetFullViewFromId(c.TemplateId))).Where(c =>
                        {
                            if (c.Item2 is TemplateStringFullView fv && (fv.Genus?.Contains("e_wuqi") ?? false)) return true;
                            return false;
                        }).Count();
                        if (_AchievementManager.GetTemplateById(Guid.Parse("0f83e776-7c47-4fc6-b27b-c5f14f1b158f")) is TemplateStringFullView achiTt //拥有武器的数量成就
                            && _AchievementManager.GetOrCreate(gc, achiTt) is GameAchievement achi)
                        {
                            //拥有装备的数量变化事件（含武器和坐骑） 35f7d6af-503a-4292-83e7-610a6155ce88
                            if (achi.Count < nv)   //若武器数量增加
                                _EventManager.SendEventWithNewValue(Guid.Parse("35f7d6af-503a-4292-83e7-610a6155ce88"), nv, context);
                        }
                    }
                    if (tt.Genus.Contains("e_zuoqi"))   //若拥有坐骑的数量变化了 2db6d491-67f3-49ea-bfa1-16413a0b9999
                    {
                        var achi = _AchievementManager.GetOrCreate(context.GameChar, Guid.Parse("1c5e6055-d754-4f9d-83c1-c2b31238afa1"));  //拥有动物数量成就
                        var nv = _EntityManager.GetAllEntity(gc).Select(c => (c, _TemplateManager.GetFullViewFromId(c.TemplateId))).Where(c =>
                        {
                            if (c.Item2 is TemplateStringFullView fv && (fv.Genus?.Contains("e_zuoqi") ?? false)) return true;
                            return false;
                        }).Count();
                        if (nv > achi.Count)    //若坐骑数量增加
                            _EventManager.SendEventWithNewValue(Guid.Parse("2db6d491-67f3-49ea-bfa1-16413a0b9999"), nv, context);
                    }
                }

                if (tt.Genus is not null && (tt.Genus.Contains("e_zuoqi") || tt.Genus.Contains("equipskin")))  //获得了坐骑或皮肤
                {
                    var achiTt = _AchievementManager.Templates.Where(c => c.Value.Genus.Contains("tj_jiangli")).FirstOrDefault(c => //实体对应的图鉴成就
                    {
                        var ary = c.Value.Achievement?.TjIns;
                        if (ary is null || ary.Length <= 0) return false;
                        var entities = new GameEntity[] { entity };
                        var tmpInItems = ary.Select(c => _BlueprintManager.Transformed(c, entities));
                        return _BlueprintManager.GetMatches(entities, tmpInItems, 1).All(c => c.Item1 is not null);
                    }).Value;
                    if (achiTt is null) continue;
                    var achi = _AchievementManager.GetOrCreate(gc, achiTt);
                    if (!_AchievementManager.IsValid(achi, gc, now)) continue;    //若处于无效状态
                    var oldLv = achi.Level;
                    achi.Count += Math.Max(1, achi.Count);
                    if (!_AchievementManager.RefreshState(achi, gc, now)) continue;
                    if (achi.Level > oldLv)    //若发生了变化
                    {
                        _AchievementManager.InvokeAchievementChanged(new AchievementChangedEventArgs { Achievement = achi });
                    }
                }
            }
            if (gc is not null)
            {
                //A934D181-087E-4BD1-BD90-8EBA1020AA99	收集皮肤总数的成就 Gid 1602
                {
                    if (_AchievementManager.GetTemplateById(Guid.Parse("A934D181-087E-4BD1-BD90-8EBA1020AA99")) is TemplateStringFullView achiTT &&
                        _AchievementManager.GetOrCreate(gc, achiTT) is GameAchievement achi)
                    {
                        var count = _EntityManager.GetAllEntity(gc).Where(c =>  //获取皮肤数量
                        {
                            if (_TemplateManager.GetFullViewFromId(c.TemplateId) is not TemplateStringFullView tt) return false;
                            return tt.Gid / 100_000 == 1602;
                        }).Count();
                        var inc = count - achi.Count;
                        if (inc > 0) _AchievementManager.RaiseEventIfChanged(achi, inc, gc, now);
                    }
                }
                //ce076015-1938-4df6-aa97-d165cbe32547	玩家镶嵌装备的等级数量变化事件（镶嵌物品等级总和） "Genus":["gs_equipslot"]
                if (_TemplateManager.GetTemplatesFromGenus("gs_equipslot") is IEnumerable<TemplateStringFullView> tts)   //获取所有装备槽模板
                {

                    var ttIds = new HashSet<Guid>(tts.Select(c => c.TemplateId));   //所有装备槽模板Id集合
                    var things = gc.GetThing().GetAllChildren().Where(c => ttIds.Contains(c.Parent?.ExtraGuid ?? Guid.Empty));   //所有装备的数据对象
                    var entitis = things.Select(c => _EntityManager.GetEntity(c));
                    var nv = entitis.Sum(c => c.Level); //现有总穿戴的装备等级

                    var achi = _AchievementManager.GetOrCreate(context.GameChar, Guid.Parse("2d023b02-fb74-4320-9ee4-b6c761938fbe"));

                    if (achi?.Count < nv)
                        _EventManager.SendEventWithNewValue(Guid.Parse("ce076015-1938-4df6-aa97-d165cbe32547"), nv, context);
                }
                //b528646c-070a-4776-a408-825cf4f437ed	角色经验变化事件
                var ss = Guid.Parse("1f31807a-f633-4d3a-8e8e-382ad105d061");
                var entityExp = e.Entities.FirstOrDefault(c => c.TemplateId == ss);
                if (entityExp is not null)
                {
                    _EventManager.SendEventWithNewValue(Guid.Parse("6afdddd0-b98d-45fc-8f8d-41fb1f929cf8"), entityExp.Count, context);
                }
            }
        }

    }
}
