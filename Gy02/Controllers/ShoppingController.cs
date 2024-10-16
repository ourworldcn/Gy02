using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
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
        /// <param name="shoppingManager"></param>
        /// <param name="templateManager"></param>
        /// <param name="searcherManager"></param>
        public ShoppingController(GameAccountStoreManager gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager, GameEntityManager entityManager,
            GameShoppingManager shoppingManager, GameTemplateManager templateManager, GameSearcherManager searcherManager)
        {
            _GameAccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _EntityManager = entityManager;
            _ShoppingManager = shoppingManager;
            _TemplateManager = templateManager;
            _SearcherManager = searcherManager;
        }

        GameAccountStoreManager _GameAccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;
        GameEntityManager _EntityManager;
        GameShoppingManager _ShoppingManager;
        GameTemplateManager _TemplateManager;
        GameSearcherManager _SearcherManager;

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
        /// <response code="401">令牌无效。</response>  
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
        /// 按指定TId获取商品配置数据。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetShoppingItemsByIdsReturnDto> GetShoppingItemsByIds(GetShoppingItemsByIdsParamsDto model)
        {
            var result = new GetShoppingItemsByIdsReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            //获取基础集合
            IEnumerable<TemplateStringFullView> baseColl;

            baseColl = _TemplateManager.Id2FullView.Where(c => c.Value.ShoppingItem is not null && model.TIds.Contains(c.Value.TemplateId)).Select(c => c.Value);
            //刷新金猪周期
            if (_ShoppingManager.IsChanged(gc, "gs_jinzhu")) _ShoppingManager.JinzhuChanged(gc);
            //刷新礼包周期
            if (_ShoppingManager.IsChanged(gc, "gs_leijilibao")) _ShoppingManager.LibaoChanged(gc);
            //过滤
            DateTime nowUtc = OwHelper.WorldNow;    //当前
            List<(TemplateStringFullView, DateTime)> list = new List<(TemplateStringFullView, DateTime)>();
            foreach (var item in baseColl)  //遍历基础集合
            {
                var b = _ShoppingManager.IsMatchWithoutBuyed(gc, item, nowUtc, out var startUtc, 2);
                if (!b) continue;   //若不符合条件
                list.Add((item, startUtc));
            }
            var coll1 = list.Where(c => c.Item1.Genus.Contains("gs_meirishangdian")).ToArray();
            result.ShoppingItemStates.AddRange(list.Select(c =>
            {
                var tmp = new ShoppingItemStateDto
                {
                    TId = c.Item1.TemplateId,
                    StartUtc = c.Item2,
                    EndUtc = c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod,
                    BuyedCount = gc.ShoppingHistoryV2.Where(history => history.TId == c.Item1.TemplateId && history.WorldDateTime >= c.Item2 && history.WorldDateTime < c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod).Sum(c => c.Count),
                };
                var per = _SearcherManager.GetPeriodIndex(c.Item1.ShoppingItem.Ins, gc, out _);
                if (per.HasValue) //若有自周期
                {
                    var newBuyedCount = gc.ShoppingHistoryV2.Where(history => history.TId == c.Item1.TemplateId)
                        .Where(c => c.PeriodIndex == per).Sum(c => c.Count);
                    tmp.BuyedCount = newBuyedCount;
                }
                return tmp;
            }));

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
            //购买的消耗商品
            //1d8542d3-da32-445e-9845-c9dc25e15cd5 修改名称商品购买
            var command = new ShoppingBuyCommand
            {
                GameChar = gc,
                Count = 1,
                ShoppingItemTId = Guid.Parse("1d8542d3-da32-445e-9845-c9dc25e15cd5"),
            };

            _SyncCommandManager.Handle(command);
            if (command.HasError)
            {
                result.FillErrorFrom(command);
                return result;
            }
            _Mapper.Map(command.Changes, result.Changes);
            //更新名字
            var ovDisplayName = gc.DisplayName;
            gc.DisplayName = model.DisplayName;
            changes.MarkChanges(gc, nameof(gc.DisplayName), ovDisplayName, gc.DisplayName);
            //更新更改次数
            var ov = gc.RenameCount;
            gc.RenameCount++;
            changes.MarkChanges(gc, nameof(gc.RenameCount), ov, gc.RenameCount);
            //记录变化数据
            var tmp = _Mapper.Map<List<GamePropertyChangeItemDto>>(changes);
            result.Changes.AddRange(tmp);
            _GameAccountStore.Save(gc.GetUser().Key);
            return result;
        }

        #region 法币购买相关

        /// <summary>
        /// 创建法币购买订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateOrderV2ReturnDto> CreateOrderV2(CreateOrderV2ParamsDto model)
        {
            var result = new CreateOrderV2ReturnDto();
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            return result;
        }
        #endregion 法币购买相关
    }

    /// <summary>
    /// 创建法币购买订单功能的参数封装类。
    /// </summary>
    public class CreateOrderV2ParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 渠道标识，针对每个不同渠道会给出不同命名，通常是 渠道名/地区/平台，当然也可能不区分平台。
        /// </summary>
        public string Channel { get; set; }
    }

    /// <summary>
    /// 创建法币购买订单功能的返回值封装类。
    /// </summary>
    public class CreateOrderV2ReturnDto : ReturnDtoBase
    {
    }
}
