using System;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class StringExtensions
    {

        public static Boolean IsEmpty(this String s)
            => String.IsNullOrWhiteSpace(s);

        public static String Quote(this String s)
            => $"\"{s}\"";

    }
}
