using Discord.Commands;

namespace Kuuhaku.Commands.Classes
{
    public static class CommandServiceExtensions
    {

        public static void AddTypeReader<TType, TTypeReader>(this CommandService cs)
            where TTypeReader : TypeReader, new()
            => cs.AddTypeReader<TType>(new TTypeReader());

    }
}
