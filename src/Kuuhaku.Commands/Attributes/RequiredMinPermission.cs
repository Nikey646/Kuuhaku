using System;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Kuuhaku.Commands.Attributes
{
    public class RequiredMinPermission : PreconditionAttribute
    {
        public CommandPermissions Permissions { get; }

        private UInt64[] DeveloperIds = new UInt64[]
        {
            92647530919116800, // Nikey
            115080760439996433, // Mushoku
        };

        public RequiredMinPermission(CommandPermissions permissions)
        {
            this.Permissions = permissions;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext iContext, CommandInfo command, IServiceProvider services)
        {
            var context = iContext as KuuhakuCommandContext;
            if (context == default)
                return PreconditionResult.FromError("Failed to cast context");

            var appInfo = await context.Client.GetApplicationInfoAsync();

            if (context.IsPrivate)
            {
                if (this.Permissions == CommandPermissions.Everyone)
                    return PreconditionResult.FromSuccess();
                if (this.Permissions <= CommandPermissions.BotOwner && context.User.Id == appInfo?.Owner?.Id)
                    return PreconditionResult.FromSuccess();
                if (this.Permissions <= CommandPermissions.Developer && this.DeveloperIds.Contains(context.User.Id))
                    return PreconditionResult.FromSuccess();
                return PreconditionResult.FromError("Command requires higher permissions than vailable in a direct message");
            }

            if (!(context.User is SocketGuildUser user))
                return PreconditionResult.FromError("Failed to get User as a Guild User");

            var repoistory = services.GetRequiredService<PermissionsRepository>();
            var moderatorRoles = await repoistory.GetRolesAsync(context.Guild, CommandPermissions.Moderator.ToString());
            var adminRoles = await repoistory.GetRolesAsync(context.Guild, CommandPermissions.Admin.ToString());

            if (this.Permissions <= CommandPermissions.Developer && this.DeveloperIds.Contains(context.User.Id))
                return PreconditionResult.FromSuccess();
            if (this.Permissions <= CommandPermissions.BotOwner && context.User.Id == appInfo?.Owner?.Id)
                return PreconditionResult.FromSuccess();
            if (this.Permissions <= CommandPermissions.ServerOwner && user.Id == context.Guild.Owner.Id)
                return PreconditionResult.FromSuccess();
            if (this.Permissions <= CommandPermissions.Admin && adminRoles.Intersect(user.Roles.Select(r => r.Id)).Any())
                return PreconditionResult.FromSuccess();
            if (this.Permissions <= CommandPermissions.Moderator && moderatorRoles.Intersect(user.Roles.Select(r => r.Id)).Any())
                return PreconditionResult.FromSuccess();
            if (this.Permissions == CommandPermissions.Everyone)
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError("Insufficient Permissions");
        }
    }
}
