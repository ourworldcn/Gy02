using Microsoft.Extensions.Options;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    public class AccountManagerOptions : IOptions<AccountManagerOptions>
    {
        public AccountManagerOptions()
        {
        }

        public AccountManagerOptions Value => this;


    }

    public class AccountManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AccountManager(AccountManagerOptions options, GameObjectCache cache)
        {
            Options = options;
            Cache = cache;
            Initialize();
        }

        public AccountManagerOptions Options { get; set; }

        public GameObjectCache Cache { get; }

        /// <summary>
        /// 内部初始化函数。
        /// </summary>
        private void Initialize()
        {
            var key = SingletonLocker.Intern(Guid.NewGuid().ToString());
        }

        ConcurrentDictionary<Guid, VirtualThing> _Token2Char = new ConcurrentDictionary<Guid, VirtualThing>();

        public DisposeHelper<string> Load(Guid charId, out VirtualThing? thing)
        {
            var result = DisposeHelper.Empty<string>();
            thing = default;
            return result;
        }
    }
}
