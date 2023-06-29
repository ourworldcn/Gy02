using GY02.Managers;
using GY02.Publisher;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class GetServerDictionaryCommand : SyncCommandBase
    {
        //public GameChar GameChar { get; set; }

        /// <summary>
        /// 要获取的键值名。
        /// </summary>
        public List<string> Names { get; set; } = new List<string>();

        /// <summary>
        /// 返回的键值字典，仅包含参数中需要的部分。
        /// </summary>
        public Dictionary<string, string> Result { get; set; } = new Dictionary<string, string>();
    }

    public class GetServerDictionaryHandler : SyncCommandHandlerBase<GetServerDictionaryCommand>/*, IGameCharHandler<GetServerDictionaryCommand>*/
    {
        public GetServerDictionaryHandler(GameAccountStoreManager accountStore, GY02UserContext dbContext)
        {
            AccountStore = accountStore;
            DbContext = dbContext;
        }

        public GameAccountStoreManager AccountStore { get; }

        public GY02UserContext DbContext { get; }

        public override void Handle(GetServerDictionaryCommand command)
        {
            var item = DbContext.ServerConfig.Find(ProjectContent.ServerDictionaryName);
            if (item is null)
            {
                item = new ServerConfigItem { Name = ProjectContent.ServerDictionaryName };
                DbContext.Add(item);
            }
            var dic = string.IsNullOrWhiteSpace(item.Value) ? new Dictionary<string, string> { } : (Dictionary<string, string>)JsonSerializer.Deserialize(item.Value, typeof(Dictionary<string, string>));
            OwHelper.Copy(dic, command.Result, c => command.Names.Contains(c));
        }
    }
}
