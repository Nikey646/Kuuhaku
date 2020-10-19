using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Modules
{
    [Group("config"), RequireContext(ContextType.Guild)]
    public class GuildConfigurationModule : KuuhakuModule
    {

        private readonly String[] PermanentModules = new[] {"GuildConfigurationModule", "StandardModule", "PermissionsModule"};

        private readonly GuildConfigRepository _repository;
        private readonly CommandService _commandService;

        public GuildConfigurationModule(GuildConfigRepository repository, CommandService commandService)
        {
            this._repository = repository;
            this._commandService = commandService;
        }

        [Command, Alias("info")]
        public async Task GetInfoAsync()
        {
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithDescription("The current settings for this server is...")
                .WithField("Prefix", this.Config.Prefix)
                .WithField("Command Seperator", this.Config.CommandSeperator)
                .WithFooter(this.Context);

            await this.ReplyAsync(embed);
        }

        [Command("prefix")]
        public async Task GetPrefixAsync()
        {
            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithDescription($"The prefix for this server is {this.Context.Config.Prefix.MdBold()}")
                .WithFooter(this.Context);
            await this.ReplyAsync(embed);
        }

        [Command("prefix")]
        public async Task SetPrefixAsync(String newPrefix)
        {
            var config = await this._repository.GetAsync(this.Guild);

            var oldPrefix = config.Prefix;
            config.Prefix = newPrefix;
            await this._repository.UpdateAsync(this.Guild, config);

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithDescription($"The prefix for this server is now {config.Prefix.MdBold()} (Was previously {oldPrefix.MdBold()})")
                .WithFooter(this.Context);
            await this.ReplyAsync(embed);
        }

        [Command("blacklist user")]
        public async Task BlacklistUser(IUser user)
        {
            var isBlacklisted = await this._repository.IsUserBlacklisted(this.Guild, user);

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(this.Context);

            if (isBlacklisted)
            {
                await this._repository.RemoveBlacklistUser(this.Guild, user);
                await this.ReplyAsync(embed.WithDescription($"{user.Mention} is no longer blacklisted from using commands."));
            }
            else
            {
                await this._repository.AddBlacklistedUser(this.Guild, user);
                await this.ReplyAsync(embed.WithDescription($"{user.Mention} is now blacklisted from using commands."));
            }
        }

        [Command("blacklist user"), Alias("blacklist users", "blacklisted users", "blacklisted user")]
        public async Task ViewBlacklistedUsers()
        {
            var blacklistedUsers =  await this._repository.GetBlacklistedUsers(this.Guild);

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(this.Context);

            const String prefixMessage = "The following users are blacklisted from using my commands: ";
            var messageLength = prefixMessage.Length;
            var blacklistedMentions = new List<String>();

            foreach (var blacklistedUser in blacklistedUsers.Where(v => v > 0))
            {
                var mention = MentionUtils.MentionUser(blacklistedUser);
                messageLength += mention.Length + 2;
                if (messageLength > EmbedBuilder.MaxDescriptionLength - 20)
                {
                    blacklistedMentions.Add("More…");
                }

                blacklistedMentions.Add(mention);
            }

            if (blacklistedMentions.Count == 0)
            {
                await this.ReplyAsync(
                    embed.WithDescription("There are no users who are blacklisted from using my commands"));
                return;
            }

            await this.ReplyAsync(embed.WithDescription(prefixMessage + blacklistedMentions.Humanize()));
        }

        [Command("blacklist module")]
        public async Task BlacklistModule(String moduleName)
        {
            var input = moduleName;
            moduleName = moduleName.Dehumanize();
            if (!moduleName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
                moduleName += "Module";

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(this.Context);

            var modules = this._commandService.Modules.Select(m => m.Name);
            if (!modules.Contains(moduleName, StringComparer.OrdinalIgnoreCase))
            {
                await this.ReplyAsync(
                    embed.WithDescription($"There is no such module as {input.MdBold().Quote()}"));
                return;
            }

            var friendlyName = moduleName.Replace("Module", "").Humanize(LetterCasing.Title);

            if (this.PermanentModules.Contains(moduleName))
            {
                await this.ReplyAsync(embed.WithDescription($"You cannot disable {friendlyName.MdBold().Quote()}"));
                return;
            }

            var isBlacklisted = await this._repository.IsModuleBlacklisted(this.Guild, moduleName);
            if (!isBlacklisted)
            {
                await this._repository.AddBlacklistedModule(this.Guild, moduleName);
                await this.ReplyAsync(embed.WithDescription(
                    $"{friendlyName.MdBold().Quote()} has now been blacklisted.\n" +
                    $"Users in this server will no longer be able to use it."));
            }
            else
            {
                await this._repository.RemoveBlacklistModule(this.Guild, moduleName);
                await this.ReplyAsync(embed.WithDescription(
                    $"{friendlyName.MdBold().Quote()} has is no longer blacklisted.\n" +
                    $"Users in this server will now be able to use it."));
            }
        }

        [Command("blacklist module"), Alias("blacklist modules", "blacklisted modules", "blacklisted module")]
        public async Task BlacklistModule()
        {
            var availableModules = this._commandService.Modules.Select(m => m.Name.Replace("Module", ""));
            var blacklistedModules = await this._repository.GetBlacklistedModules(this.Guild);

            var embed = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithDescription(
                    $"The available Modules are: \n{availableModules.Humanize(m => m.Humanize(LetterCasing.Title).MdBold())}\n\n" +
                    $"The currently Blacklisted Modules are: \n{blacklistedModules.Humanize(m => m.MdBold())}")
                .WithFooter(this.Context);

            await this.ReplyAsync(embed);
        }

    }
}
