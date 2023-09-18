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
        public AchievementEventHandler(GameEntityManager entityManager, GameAchievementManager achievementManager, GameBlueprintManager blueprintManager, GameTemplateManager templateManager)
        {
            _EntityManager = entityManager;
            _AchievementManager = achievementManager;
            _BlueprintManager = blueprintManager;
            Initialize();
            _TemplateManager = templateManager;
        }

        private void Initialize()
        {
            _EntityManager.EntityChanged += _EntityManager_EntityChanged;
        }

        GameEntityManager _EntityManager;
        GameAchievementManager _AchievementManager;
        GameBlueprintManager _BlueprintManager;
        GameTemplateManager _TemplateManager;

        /// <summary>
        /// 所有装备的类属集合。
        /// </summary>
        static HashSet<string> _Equ = new HashSet<string>() { "e_wuqi", "e_yifu", "e_yaodai", "e_shoutao", "e_xiezi", "e_zuoqi" };

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
            if (e.Entities.Any(c => c.TemplateId == ProjectContent.GuanggaoCurrenyTId && c.Count > 0))
            {
                //192db3ea-2147-4743-849e-9c20236c7771	看广告数量成就
                _AchievementManager.RaiseEventIfChanged(Guid.Parse("192db3ea-2147-4743-849e-9c20236c7771"), 1, gc, now);
                //cd6f3fde-0b5e-4c53-9b30-cb9c6da8fc3d	开服活动1日成就-累计看广告次数
                _AchievementManager.RaiseEventIfChanged(Guid.Parse("cd6f3fde-0b5e-4c53-9b30-cb9c6da8fc3d"), 1, gc, now);
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
                        achi.Count = Math.Max(achi.Count, nv);  //只增不减
                        if (!_AchievementManager.RefreshState(achi, gc, now)) continue;
                        if (achi.Level > oldLv)    //若发生了变化
                        {
                            _AchievementManager.InvokeAchievementChanged(new AchievementChangedEventArgs { Achievement = achi });
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
                            var inc = nv - achi.Count;
                            if (inc > 0) _AchievementManager.RaiseEventIfChanged(achi, inc, gc, now);
                        }
                    }
                }

                if (tt.Genus is not null && (tt.Genus.Contains("e_zuoqi") || tt.Genus.Contains("equipskin")))  //获得了坐骑或皮肤
                {
                    var achiTt = _AchievementManager.Templates.Where(c => c.Value.Genus.Contains("tj_jiangli")).FirstOrDefault(c => //实体对应的图鉴成就
                    {
                        var ary = c.Value.Achievement?.TjIns;
                        if (ary is null || ary.Length <= 0) return false;
                        var entities = new GameEntity[] { entity };
                        return _BlueprintManager.IsValid(ary, entities);
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
                //1c5e6055-d754-4f9d-83c1-c2b31238afa1	拥有动物数量 Gid 12xx02
                {
                    if (_AchievementManager.GetTemplateById(Guid.Parse("1c5e6055-d754-4f9d-83c1-c2b31238afa1")) is TemplateStringFullView achiTT &&
                        _AchievementManager.GetOrCreate(gc, achiTT) is GameAchievement achi)
                    {
                        var count = _EntityManager.GetAllEntity(gc).Where(c =>  //获取动物数量
                        {
                            if (_TemplateManager.GetFullViewFromId(c.TemplateId) is not TemplateStringFullView tt) return false;
                            return tt.GetCatalog1() == 12 && tt.GetCatalog3() == 2;
                        }).Count();
                        var inc = count - achi.Count;
                        if (inc > 0) _AchievementManager.RaiseEventIfChanged(achi, inc, gc, now);
                    }
                }
                //A934D181-087E-4BD1-BD90-8EBA1020AA99	收集皮肤总数的成就 Gid 1602
                {
                    if (_AchievementManager.GetTemplateById(Guid.Parse("A934D181-087E-4BD1-BD90-8EBA1020AA99")) is TemplateStringFullView achiTT &&
                        _AchievementManager.GetOrCreate(gc, achiTT) is GameAchievement achi)
                    {
                        var count = _EntityManager.GetAllEntity(gc).Where(c =>  //获取皮肤数量
                        {
                            if (_TemplateManager.GetFullViewFromId(c.TemplateId) is not TemplateStringFullView tt) return false;
                            return tt.Gid / 100000 == 1602;
                        }).Count();
                        var inc = count - achi.Count;
                        if (inc > 0) _AchievementManager.RaiseEventIfChanged(achi, inc, gc, now);
                    }
                }
                //2d023b02-fb74-4320-9ee4-b6c761938fbe	全部镶嵌装备的等级成就 "Genus":["gs_equipslot"]
                do
                {
                    if (_TemplateManager.GetTemplatesFromGenus("gs_equipslot") is not IEnumerable<TemplateStringFullView> tts) break;  //获取所有装备槽模板
                    var ttIds = new HashSet<Guid>(tts.Select(c => c.TemplateId));   //所有装备槽模板Id集合
                    var things = gc.GetThing().GetAllChildren().Where(c => ttIds.Contains(c.Parent?.ExtraGuid ?? Guid.Empty));   //所有装备的数据对象
                    var entitis = things.Select(c => _EntityManager.GetEntity(c));
                    var nv = entitis.Sum(c => c.Level); //现有总穿戴的装备等级

                    var achiTId = Guid.Parse("2d023b02-fb74-4320-9ee4-b6c761938fbe");

                    _AchievementManager.RaiseEventIfIncrease(achiTId, nv, gc, OwHelper.WorldNow);
                } while (false);
            }
        }

    }
}
