using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwDbBase
{
    public class DataObjectManagerOptions : IOptions<DataObjectManagerOptions>
    {
        public DataObjectManagerOptions()
        {
        }

        public DataObjectManagerOptions Value => this;
    }

    /// <summary>
    /// 数据对象管理器。
    /// 缓存数据对象。
    /// 可以设置尽量即时保存。
    /// 在驱逐时保存。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class DataObjectManager
    {
        public DataObjectManager(DataObjectManagerOptions options)
        {
            Options = options;
        }

        public DataObjectManagerOptions Options { get; set; }

    }
}
