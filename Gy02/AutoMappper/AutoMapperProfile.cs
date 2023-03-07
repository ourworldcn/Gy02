using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using OW.Game.Entity;
using OW.Game.Store;

namespace Gy02.AutoMappper
{
    /// <summary>
    /// AutoMapperProfile配置类。
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AutoMapperProfile()
        {
            //基础数据
            CreateMap<VirtualThing, VirtualThingDto>();
            //命令相关
            CreateMap<CreateAccountParamsDto, CreateAccountCommand>();
            CreateMap<CreateAccountCommand, CreateAccountResultDto>();

            CreateMap<LoginParamsDto, LoginCommand>();
            CreateMap<LoginCommand, LoginReturnDto>().AfterMap((comm, dto) =>
            {
                dto.GameChar = comm.User?.GetCurrentChar();
                dto.Token = comm.User?.Token ?? Guid.Empty;
            });


        }
    }
}
