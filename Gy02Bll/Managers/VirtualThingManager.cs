﻿using AutoMapper;
using Gy02Bll.Base;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Caching;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Manager
{
    /// <summary>
    /// TODO 标记强类型对象的模板Id。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TemplateIdAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tidString"><see cref="Guid"/>的字符串表述形式。</param>
        public TemplateIdAttribute(string tidString)
        {
            _TemplateId = Guid.Parse(tidString);

            // Implement code here

        }

        /// <summary>
        /// See the attribute guidelines at 
        /// http://go.microsoft.com/fwlink/?LinkId=85236
        /// </summary>
        readonly Guid _TemplateId;
        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId
        {
            get { return _TemplateId; }
        }

    }

    public class VirtualThingManagerOptions : IOptions<VirtualThingManagerOptions>
    {
        public VirtualThingManagerOptions Value => this;
    }

    /// <summary>
    /// 虚拟物管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class VirtualThingManager : GameManagerBase<VirtualThingManagerOptions, VirtualThingManager>
    {
        public VirtualThingManager(IOptions<VirtualThingManagerOptions> options, ILogger<VirtualThingManager> logger, TemplateManager templateManager, IMapper mapper) : base(options, logger)
        {
            _TemplateManager = templateManager;

            Initialize();
            _Mapper = mapper;
        }

        void Initialize()
        {

        }

        TemplateManager _TemplateManager;

        IMapper _Mapper;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tv">创建的模板。</param>
        /// <param name="changes">记录详细变化数据的集合，省略或为null则忽略。</param>
        /// <returns></returns>
        public VirtualThing Create(TemplateStringFullView tv, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            VirtualThing result = new VirtualThing { };
#if DEBUG 
            _TemplateManager.SetTemplate(result);
#endif
            var type = TemplateManager.GetTypeFromTemplate(tv);    //获取实例类型

            var view = result.GetJsonObject(type);
            _Mapper.Map(tv, view, tv.GetType(), view.GetType()); //复制一般性属性。

            if (tv.TIdsOfCreate is not null)
                foreach (var item in tv.TIdsOfCreate) //创建所有子对象
                {
                    var sub = Create(tv);   //创建子对象
                    result.Children.Add(sub);
                    sub.Parent = result;
                    sub.ParentId = result.Id;
                    changes?.CollectionAdd(sub, result);
                }
            return result;
        }

    }

}
