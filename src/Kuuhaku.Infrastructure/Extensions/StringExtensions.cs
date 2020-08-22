using System;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static Boolean IsEmpty(this String str)
            => String.IsNullOrWhiteSpace(str);

        public static String Quote(this String str)
            => $"\"{str}\"";
    }
}
