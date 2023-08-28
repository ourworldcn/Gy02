using GY02.Managers;
using GY02.Publisher;
using OW.Game.Entity;
using OW.SyncCommand;

namespace GY02.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class AchievementShoppingBuyedHandler : SyncCommandHandlerBase<ShoppingBuyedCommand>
    {
        public AchievementShoppingBuyedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public override void Handle(ShoppingBuyedCommand command)
        {
            if (command.HasError) return;
            //体力商品的TId
            var tiliGoodsTId = new Guid[] { Guid.Parse("59bf4cb7-5bc2-48ee-98ad-2efbf116b162"), Guid.Parse("622b4572-782d-4bee-b4c5-2d512daa3714") };
            if (tiliGoodsTId.Contains(command.ShoppingItemTId)) //可能需要处理购买体力的成就
            {
                var now = OwHelper.WorldNow;
                var inc = command.Changes.Where(c =>  //获取体力增量
                {
                    if (c.Object is not GameEntity entity) return false;
                    if (entity.TemplateId != ProjectContent.PowerTId) return false;
                    return true;
                }).Select(c => (c.HasOldValue && OwConvert.TryToDecimal(c.OldValue, out var ov) ? ov : 0m, c.HasNewValue && OwConvert.TryToDecimal(c.NewValue, out var nv) ? nv : 0))
                .Sum(c => c.Item2 - c.Item1);
                if (inc > 0)
                {
                    var tt = _AchievementManager.GetTemplateById(Guid.Parse("822b1d80-70fe-417d-baea-e9c2aacbdcd8"));
                    if (tt is null) { command.FillErrorFromWorld(); }
                    var achi = _AchievementManager.GetOrCreate(command.GameChar, tt);
                    if (achi is null) { command.FillErrorFromWorld(); }
                    if (!_AchievementManager.RaiseEventIfLevelChanged(achi, inc, command.GameChar, now)) command.FillErrorFromWorld();
                }
            }
        }
    }
}

