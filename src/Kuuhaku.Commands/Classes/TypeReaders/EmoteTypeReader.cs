using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Kuuhaku.Commands.Classes.TypeReaders
{
    public class EmoteTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, String input, IServiceProvider services)
        {
            if (Emote.TryParse(input, out var emote))
                return Task.FromResult(TypeReaderResult.FromSuccess(emote));
            if (NeoSmart.Unicode.Emoji.IsEmoji(input, 1))
                return Task.FromResult(TypeReaderResult.FromSuccess(new Emoji(input)));
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                $"{input} could not be recongized as either an Emoji or Unicode Emote"));
        }
    }
}
