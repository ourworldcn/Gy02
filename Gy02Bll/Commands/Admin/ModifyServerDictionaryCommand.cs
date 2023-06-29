using GY02.Managers;
using GY02.Publisher;
using OW.DDD;
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
    /// <summary>
    /// 修改全服配置字典的命令。仅超管或管理员才能设置。
    /// </summary>
    public class ModifyServerDictionaryCommand : SyncCommandBase, IGameCharCommand
    {

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要设置的字典，若没有指定键值则追加，如果已有指定键值则覆盖。键的长度要小于或等于64个字。
        /// </summary>
        public Dictionary<string, string> Dictionary { get; set; } = new Dictionary<string, string>();
    }

    public class ModifyServerDictionaryHandler : SyncCommandHandlerBase<ModifyServerDictionaryCommand>, IGameCharHandler<ModifyServerDictionaryCommand>
    {
        public ModifyServerDictionaryHandler(GameAccountStoreManager accountStore, GY02UserContext dbContext)
        {
            AccountStore = accountStore;
            DbContext = dbContext;
        }

        public GameAccountStoreManager AccountStore { get; }

        public GY02UserContext DbContext { get; set; }

        public override void Handle(ModifyServerDictionaryCommand command)
        {
            if (command.GameChar.Roles.All(c => c != ProjectContent.SupperAdminRole && c != ProjectContent.AdminRole))   //若无权限
            {
                command.ErrorCode = ErrorCodes.ERROR_INVALID_ACL;
                return;
            }
            var item = DbContext.ServerConfig.Find(ProjectContent.ServerDictionaryName);
            if (item is null)
            {
                item = new ServerConfigItem { Name = ProjectContent.ServerDictionaryName };
                DbContext.Add(item);
            }
            var dic = string.IsNullOrWhiteSpace(item.Value) ? new Dictionary<string, string> { } : (Dictionary<string, string>)JsonSerializer.Deserialize(item.Value, typeof(Dictionary<string, string>));
            OwHelper.Copy(command.Dictionary, dic);
            item.Value = JsonSerializer.Serialize(dic);
            DbContext.SaveChanges();
        }
    }
}
