using GY02.Managers;
using GY02.Publisher;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class ModifyClientDictionaryCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要修改实体的唯一Id。
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// 字典内的键如果已经存在则覆盖值，没有则追加。
        /// </summary>
        public Dictionary<string, string> Dictionary { get; set; } = new Dictionary<string, string>();
    }

    public class ModifyClientDictionaryHandler : SyncCommandHandlerBase<ModifyClientDictionaryCommand>, IGameCharHandler<ModifyClientDictionaryCommand>
    {
        public ModifyClientDictionaryHandler(GameAccountStoreManager accountStore, GameEntityManager entityManager)
        {
            AccountStore = accountStore;
            _EntityManager = entityManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameEntityManager _EntityManager;

        public override void Handle(ModifyClientDictionaryCommand command)
        {
            var key = ((IGameCharHandler<ModifyClientDictionaryCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<ModifyClientDictionaryCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var entities = _EntityManager.GetAllEntity(command.GameChar);
            var entity = entities.FirstOrDefault(c => c.Id == command.EntityId);
            if (entity is null)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"无法找到指定Id的实体，Id={command.EntityId}";
                return;
            }
            foreach (var item in command.Dictionary)
            {
                if (entity.ClientDictionary.TryGetValue(item.Key, out var val))   //若存在指定的键值
                {

                    //command.Changes.Add(new OW.Game.PropertyChange.GamePropertyChangeItem<object>
                    //{
                    //    Object = entity,
                    //    PropertyName = nameof(entity.ClientDictionary),
                    //    HasOldValue = true,

                    //    HasNewValue = true,
                    //    NewValue = item.Value,
                    //});
                }
                else //若新加键值
                {
                    //command.Changes.Add(new OW.Game.PropertyChange.GamePropertyChangeItem<object>
                    //{
                    //    Object = entity,
                    //    PropertyName = nameof(entity.ClientDictionary),
                    //    HasOldValue = false,
                    //    HasNewValue = true,
                    //    NewValue = item.Value,
                    //});
                }
                entity.ClientDictionary[item.Key] = item.Value;
            }
            AccountStore.Save(key);
        }
    }
}
