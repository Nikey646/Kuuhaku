using System;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class StringExtensions
    {

        public static String Quote(this String s)
            => $"\"{s}\"";

    }
}
