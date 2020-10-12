using System;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class MarkdownStringExtensions
    {
        public static String MdBold(this String str)
            => $"**{str}**";
    }
}
