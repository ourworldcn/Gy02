using AutoMapper;
using AutoMapper.Execution;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            //账号角色
            CreateMap<TemplateStringFullView, GameUser>().AfterMap(FillSeq);
            CreateMap<TemplateStringFullView, GameChar>().AfterMap(FillSeq);
            //槽
            CreateMap<TemplateStringFullView, GameSlot<GameItem>>().AfterMap(FillSeq);
            CreateMap<TemplateStringFullView, GameSlot<GameEquipment>>().AfterMap(FillSeq);
            //装备道具
            CreateMap<TemplateStringFullView, GameEquipment>().AfterMap(FillSeq);
            CreateMap<TemplateStringFullView, GameItem>().AfterMap(FillSeq);
        }

        /// <summary>
        /// 填充序列属性。
        /// </summary>
        /// <param name="view"></param>
        /// <param name="obj"></param>
        static void FillSeq(TemplateStringFullView view, object obj)
        {
            var srcs = TypeDescriptor.GetProperties(view).OfType<PropertyDescriptor>();
            var dests = TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>();
            var coll = from src in srcs
                       join dest in dests
                       on src.Name equals dest.Name
                       where src.PropertyType.IsAssignableTo(typeof(IList<decimal>)) && dest.PropertyType == typeof(decimal)
                       select (src, dest);
            var lv = Convert.ToInt32(dests.FirstOrDefault(c => c.Name == "Level")?.GetValue(obj) ?? 0m);   //等级
            foreach (var item in coll)
            {
                var list = item.src.GetValue(view) as IList<decimal>;
                item.dest.SetValue(obj, list[lv]);
            }
        }
    }
}
