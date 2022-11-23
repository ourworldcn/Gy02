using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    public class Gy02TemplateManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        public Gy02TemplateManager(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

    }
}
