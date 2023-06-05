using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Mail
{
    public class MailsSendedCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 发送的邮件。
        /// </summary>
        public List<GameMail> Mails { get; set; } = new List<GameMail>();
    }

    public class MailsSendedHandler : SyncCommandHandlerBase<MailsSendedCommand>
    {
        public MailsSendedHandler(UdpServerManager udpServerManager, GameAccountStore accountStore)
        {
            _UdpServerManager = udpServerManager;
            _AccountStore = accountStore;
        }

        UdpServerManager _UdpServerManager;
        GameAccountStore _AccountStore;

        public override void Handle(MailsSendedCommand command)
        {
            foreach (var mail in command.Mails)
            {
                var charId = Guid.Parse(mail.To);
                var key = _AccountStore.CharId2Key.GetValueOrDefault(charId);
                var data = new MailArrivedDto { };
                data.MailIds.Add(mail.Id);
                try
                {
                    _UdpServerManager.SendObject(command.GameChar.GetUser().Token, data);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
