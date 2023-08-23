using AutoMapper;
using GY02.Managers;
using GY02.Publisher;
using OW.Game.Entity;
using OW.Game.Store;

namespace GY02.Handler
{
    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class AchievementChangedHandler
    {
        /// <summary>
        /// 
        /// </summary>
        public AchievementChangedHandler(GameAchievementManager achievementManager, UdpServerManager udpServerManager, IMapper mapper, GameEntityManager entityManager)
        {
            _AchievementManager = achievementManager;
            _UdpServerManager = udpServerManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            Initialize();
        }

        void Initialize()
        {
            _AchievementManager.AchievementChanged += _AchievementManager_AchievementChanged;
        }

        private void _AchievementManager_AchievementChanged(object? sender, AchievementChangedEventArgs e)
        {
            var guThing = e.Achievement.GetThing()?.GetAncestor(c => (c as IDbQuickFind)?.ExtraGuid == ProjectContent.UserTId) as VirtualThing; //用户的数据对象
            if (guThing is null) return;
            var gc = guThing.GetJsonObject<GameUser>();
            if (gc is null) return;
            var token = gc.Token;
            _UdpServerManager.SendObject(token, new AchievementChangedDto { Achievement = _Mapper.Map<GameAchievementDto>(e.Achievement) });
        }

        GameAchievementManager _AchievementManager;
        UdpServerManager _UdpServerManager;
        GameEntityManager _EntityManager;
        IMapper _Mapper;
    }
}
