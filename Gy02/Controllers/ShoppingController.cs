using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System.Text.Json;

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
        public ShoppingController(GameAccountStoreManager gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager)
        {
            _GameAccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
        }

        GameAccountStoreManager _GameAccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;

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
    }

}
