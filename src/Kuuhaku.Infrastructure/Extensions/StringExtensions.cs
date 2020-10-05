using System;
using System.Net;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static Boolean IsEmpty(this String str)
            => String.IsNullOrWhiteSpace(str);

        public static String Quote(this String str)
            => $"\"{str}\"";

        public static String UrlEncode(this String str)
            => WebUtility.UrlEncode(str);

        public static String UrlDecode(this String str)
            => WebUtility.UrlDecode(str);
    }
}
