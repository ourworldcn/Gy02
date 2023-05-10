using GY02.Publisher;
using GY02.Templates;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    public static class VirtualThingExtensions
    {
        public const string TemplateKeyName = "Template";

        public static VirtualThing GetGameCharThing(this VirtualThing thing)
        {
            for (var tmp = thing; tmp is not null; tmp = tmp.Parent)
            {
                if (tmp.ExtraGuid == ProjectContent.CharTId)
                    return tmp;
            }
            return null;
        }

        /// <summary>
        /// 设置虚拟物使用的模板。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="template"></param>
        public static void SetTemplate(this VirtualThing thing, TemplateStringFullView template) => thing.RuntimeProperties[TemplateKeyName] = template;

        /// <summary>
        /// 获取虚拟物的模板。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public static TemplateStringFullView GetTemplate(this VirtualThing thing) => thing.RuntimeProperties.GetValueOrDefault(TemplateKeyName) as TemplateStringFullView;

        /// <summary>
        /// 获取指定虚拟物的所有子虚拟物。
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static IEnumerable<VirtualThing> GetAllChildren(this VirtualThing root)
        {
            foreach (var item in root.Children)
            {
                yield return item;
                foreach (var item2 in item.GetAllChildren())
                    yield return item2;
            }
        }


    }
}
