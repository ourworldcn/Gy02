using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Controllers
{
    /// <summary>
    /// 商城功能控制器。
    /// </summary>
    public class ShoppingController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAccountStore"></param>
        /// <param name="mapper"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="entityManager"></param>
        public ShoppingController(GameAccountStoreManager gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager, GameEntityManager entityManager)
        {
            _GameAccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _EntityManager = entityManager;
        }

        GameAccountStoreManager _GameAccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;
        GameEntityManager _EntityManager;

#if DEBUG
        /// <summary>
        /// 获取商品项结构。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GameShoppingItem> GetShoppingItem()
        {
            return Ok();
        }

#endif
        /// <summary>
        /// 获取指定商品配置数据。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetShoppingItemsReturnDto> GetShoppingItems(GetShoppingItemsParamsDto model)
        {
            var result = new GetShoppingItemsReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetShoppingItemsCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 累计签到。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LeijiQiandaoReturnDto> LeijiQiandao(LeijiQiandaoParamsDto model)
        {
            var result = new LeijiQiandaoReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new LeijiQiandaoCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 购买指定商品。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ShoppingBuyReturnDto> ShoppingBuy(ShoppingBuyParamsDto model)
        {
            var result = new ShoppingBuyReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new ShoppingBuyCommand
            {
                GameChar = gc,
            };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            if (result.Changes?.Count > 0) result.HasError = false; //对多个购买物品时，买一次成功就算成功
            return result;
        }

        /// <summary>
        /// 客户端发起创建一个订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateOrderReturnDto> CreateOrder(CreateOrderParamsDto model)
        {
            var result = new CreateOrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new CreateOrderCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 获取订单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="db">数据库访问上下文。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetShoppingOrderReturnDto> GetShoppingOrder(GetShoppingOrderParamsDto model, [FromServices] GY02UserContext db)
        {
            var result = new GetShoppingOrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var cid = gc.Id.ToString();
            var coll = db.Set<GameShoppingOrder>().Where(c => c.CreateUtc >= model.Start && c.CreateUtc <= model.End && c.CustomerId == cid).AsEnumerable();
            foreach (var c in coll)
            {
                var order = _Mapper.Map<GameShoppingOrderDto>(c);
                result.Orders.Add(order);
            }
            return result;
        }

        /// <summary>
        /// 执行兑换码兑换功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>错误码是160表示指定的兑换码不存在。若错误码是1219则表示兑换码失效。</returns>
        [HttpPost]
        public ActionResult<RedeemReturnDto> Redeem(RedeemParamsDto model)
        {
            var result = new RedeemReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new RedeemCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 角色改名。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>用户属性变化返回在Changes里。</returns>
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<RenameCharReturnDto> RenameChar(RenameCharParamsDto model)
        {
            var result = new RenameCharReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
            if (gc.RenameCount > 0)    //若不是第一次改名
            {
                //确认有足够钻石
                var diam = new GameEntitySummary() { TId = ProjectContent.DiamTId, Count = -100 };
                if (_EntityManager.GetAllEntity(gc).FirstOrDefault(c => c.TemplateId == ProjectContent.DiamTId) is not GameEntity entity || entity.Count < 100)
                {
                    result.DebugMessage = "没有足够的钻石";
                    result.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    result.HasError = true;
                    return result;
                }
                //花费钻石
                _EntityManager.CreateAndMove(new GameEntitySummary[] { diam }, gc, changes);
            }
            //更新名字
            var ovDisplayName = gc.DisplayName;
            gc.DisplayName = model.DisplayName;
            changes.MarkChanges(gc, nameof(gc.DisplayName), ovDisplayName, gc.DisplayName);
            //更新更改次数
            var ov = gc.RenameCount;
            gc.RenameCount++;
            changes.MarkChanges(gc, nameof(gc.RenameCount), ov, gc.RenameCount);
            //记录变化数据
            _Mapper.Map(changes, result.Changes);
            _GameAccountStore.Save(gc.GetUser().GetThing().IdString);
            return result;
        }

    }

}
