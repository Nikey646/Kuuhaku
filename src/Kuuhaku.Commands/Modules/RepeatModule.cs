using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;

namespace Kuuhaku.Commands.Modules
{
    [RequireContext(ContextType.Guild)]
    public class RepeatModule : KuuhakuModule
    {
        private static readonly Type _messageType;
        private static readonly PropertyInfo _idProp;
        private static readonly PropertyInfo _channelIdProp;
        private static readonly PropertyInfo _typeProp;
        private static readonly PropertyInfo _contentProp;
        private static readonly PropertyInfo _stateProp;
        private static readonly MethodInfo _createMethod;

        private readonly RepeatRepository _repository;
        private readonly PrefixCommandHandler _commandHandler;

        static RepeatModule()
        {
            _messageType = typeof(DiscordRestClient).GetTypeInfo().Assembly.GetTypes()
                .FirstOrDefault(t => t.FullName == "Discord.API.Message");
            _idProp = _messageType.GetProperty("Id");
            _channelIdProp = _messageType.GetProperty("ChannelId");
            _typeProp = _messageType.GetProperty("Type");
            _contentProp = _messageType.GetProperty("Content");
            _createMethod = typeof(SocketMessage).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
            _stateProp =
                typeof(DiscordSocketClient).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public RepeatModule(RepeatRepository repository, PrefixCommandHandler commandHandler)
        {
            this._repository = repository;
            this._commandHandler = commandHandler;
        }

        [Command("repeat")]
        public async Task RepeatAsync()
        {
            var previousCommand = await this._repository.GetAsync(this.Guild, this.User);

            if (previousCommand == default)
                return;

            var newMessage = RecreateMessage(previousCommand.Command, this.Message, this.User, this.Channel, this.Client);
            await this._commandHandler.InternalCommandLauncher(newMessage);
        }

        private static SocketMessage RecreateMessage(String text, IMessage message, IUser user, IChannel channel,
            IDiscordClient client)
        {
            var fakeMessage = Activator.CreateInstance(_messageType);

            _idProp.SetValue(fakeMessage, message.Id);
            _channelIdProp.SetValue(fakeMessage, channel.Id);
            _typeProp.SetValue(fakeMessage, MessageType.Default);
            _contentProp.SetValue(fakeMessage, new Optional<String>(text));

            var state = _stateProp.GetValue(client);
            return _createMethod.Invoke(null, new[] {client, state, user, channel, fakeMessage}) as SocketMessage;
        }

    }
}
