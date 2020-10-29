using System;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class MarkdownStringExtensions
    {
        public static String MdBold(this String str)
            => $"**{str}**";

        public static String MdSpoiler(this String str)
            => $"||{str}||";
    }
}
