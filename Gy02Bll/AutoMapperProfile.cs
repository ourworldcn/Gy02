using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll
{
    //public class Gy02TemplateManager : TemplateManager
    //{
    //    /// <summary>
    //    /// 构造函数。
    //    /// </summary>
    //    /// <param name="service"></param>
    //    public Gy02TemplateManager(IServiceProvider service) : base(null, null)
    //    {
    //        _Service = service;
    //        Initialize();
    //    }

    //    private void Initialize()
    //    {

    //    }

    //    IServiceProvider _Service;

    //}
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {

        }
    }
}
