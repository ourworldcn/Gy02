using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands.Achievement
{
    /// <summary>
    /// 推进成就/任务系统计数的拦截器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class AchievementEventHandler
    {
        public AchievementEventHandler(GameEntityManager entityManager, GameAchievementManager achievementManager, GameBlueprintManager blueprintManager)
        {
            _EntityManager = entityManager;
            _AchievementManager = achievementManager;
            _BlueprintManager = blueprintManager;
            Initialize();
        }

        private void Initialize()
        {
            _EntityManager.EntityChanged += _EntityManager_EntityChanged;
        }

        GameEntityManager _EntityManager;
        GameAchievementManager _AchievementManager;
        GameBlueprintManager _BlueprintManager;

        /// <summary>
        /// 所有装备的类属集合。
        /// </summary>
        static HashSet<string> _Equ = new HashSet<string>() { "e_wuqi", "e_yifu", "e_yaodai", "e_shoutao", "e_xiezi", "e_zuoqi" };

        private void _EntityManager_EntityChanged(object sender, EntityChangedEventArgs e)
        {
            var now = OwHelper.WorldNow;
            foreach (var entity in e.Entities)
            {
                var tt = _EntityManager.GetTemplate(entity);    //获取模板
                if (tt is null) continue;
                var gcThing = entity.GetThing()?.GetAncestor(c => (c as IDbQuickFind)?.ExtraGuid == ProjectContent.CharTId) as VirtualThing;   //获取用户的宿主对象
                if (gcThing is null) continue;
                var gc = _EntityManager.GetEntity(gcThing) as GameChar; //获取角色对象
                if (gc is null) continue;

                if (tt.TemplateId == ProjectContent.GuanggaoCurrenyTId)    //广告币
                {
                    var achiTt = _AchievementManager.GetTemplateById(Guid.Parse("192db3ea-2147-4743-849e-9c20236c7771")); //看广告数量成就
                    if (achiTt is null) continue;
                    var achi = _AchievementManager.GetOrCreate(gc, achiTt);
                    if (!_AchievementManager.IsValid(achi, gc, now)) continue;    //若处于无效状态
                    var oldLv = achi.Level;
                    achi.Count += entity.Count;
                    if (!_AchievementManager.RefreshState(achi, gc, now)) continue;
                    if (achi.Level > oldLv)    //若发生了变化
                    {
                        _AchievementManager.InvokeAchievementChanged(new AchievementChangedEventArgs { Achievement = achi });
                    }
                }
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
                    if (tt.Genus.Contains("e_wuqi"))   //拥有武器的数量成就
                    {
                        var achiTt = _AchievementManager.GetTemplateById(Guid.Parse("0f83e776-7c47-4fc6-b27b-c5f14f1b158f")); //拥有武器的数量成就
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
        }

    }
}
