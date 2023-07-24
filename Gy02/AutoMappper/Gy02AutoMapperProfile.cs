using AutoMapper;
using GY02.Publisher;
using GY02.Templates;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System.Text.Json;

namespace GY02.AutoMappper
{
    /// <summary>
    /// AutoMapperProfile配置类。
    /// </summary>
    public class Gy02AutoMapperProfile : Profile
    {
        static Dictionary<Type, Type>? _Entyti2Dto;

        /// <summary>
        /// 将指定的实体类型转化为对应的封装类型。
        /// </summary>
        public static Dictionary<Type, Type> Entyti2Dto
        {
            get
            {
                if (_Entyti2Dto is null)
                {
                    var tmp = new Dictionary<Type, Type>{
                    { typeof(GameUser),typeof(GameUserDto)},
                    {typeof(GameChar),typeof(GameCharDto) },
                    {typeof(GameItem),typeof(GameItemDto) },
                    { typeof(GameAchievement),typeof(GameAchievementDto)},
                    {typeof(GameEquipment),typeof(GameEquipmentDto) },
                    {typeof(GameSlot<GameItem>),typeof(GameSlotDto<GameItemDto>) },
                    {typeof(GameSlot<GameEquipment>),typeof(GameSlotDto<GameEquipmentDto>) },
                    { typeof(GameSlot<GameAchievement>),typeof(GameSlot<GameAchievementDto>)},
                    };
                    Interlocked.CompareExchange(ref _Entyti2Dto, tmp, null);
                }
                return _Entyti2Dto;
            }
        }

        /// <summary>
        /// 将指定的实体转化为对应的封装数据。
        /// </summary>
        /// <param name="entity">对null则立即返回null。</param>
        /// <param name="mapper"></param>
        /// <returns>对不认识的类型则原样返回，。</returns>
        public static object? AutoMapEntity(object? entity, IMapperBase mapper)
        {
            var srcType = entity?.GetType();
            if (srcType is null)    //若源类型无法确定
                return null;
            else if (srcType == typeof(GameEntitySummary))
            {
                return mapper.Map<GameEntitySummaryDto>(entity);
            }
            else
            {
                var destType = Entyti2Dto.GetValueOrDefault(srcType);
                return destType is null ? entity : mapper.Map(entity, srcType, destType);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public Gy02AutoMapperProfile()
        {
            //基础数据
            CreateMap<VirtualThing, VirtualThingDto>();

            CreateMap<GameSlot<GameEquipment>, GameSlotDto<GameEquipmentDto>>();
            CreateMap<GameSlot<GameItem>, GameSlotDto<GameItemDto>>();
            CreateMap<GameSlot<GameAchievement>, GameSlotDto<GameAchievementDto>>();

            CreateMap<GamePropertyChangeItem<object>, GamePropertyChangeItemDto>()
                .ForMember(c => c.NewValue, opt => opt.MapFrom((src, dest, val, context) => AutoMapEntity(src.NewValue, context.Mapper)))
                .ForMember(c => c.OldValue, opt => opt.MapFrom((src, dest, val, context) => AutoMapEntity(src.OldValue, context.Mapper)))
                .AfterMap((src, dest, context) =>
                {
                    if (src.Object is GameEntityBase ogb)
                    {
                        dest.ObjectId = ogb.Id;
                        dest.TId = ogb.TemplateId;
                    }
                    else if (src.Object is VirtualThing vThing)
                    {
                        dest.ObjectId = vThing.Id;
                        dest.TId = vThing.ExtraGuid;
                    }
                });
            //命令相关

            //CreateMap<LoginCommand, LoginReturnDto>().AfterMap((comm, dto) =>
            //{
            //    dto.GameChar = comm.User.CurrentChar;
            //    dto.Token = comm.User?.Token ?? Guid.Empty;
            //});

            CreateMap<TemplateStringFullView, GameMail>();
            CreateMap<TemplateStringFullView, GameAchievement>();
            CreateMap<TemplateStringFullView, GameSlot<GameAchievement>>();

            CreateMap<GameShoppingOrder, GameShoppingOrderDto>().AfterMap((src, dest) =>
            {
                var tmp = src.BinaryArray is null ? null : JsonSerializer.Deserialize<List<GamePropertyChangeItemDto>>(src.BinaryArray);
                if (tmp != null)
                    dest.Changes.AddRange(tmp);
            });
        }
    }
}
