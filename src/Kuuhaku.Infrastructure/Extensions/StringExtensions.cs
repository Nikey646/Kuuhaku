using System;
using System.Net;
using Humanizer;

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

        public static String ReadMore(this String str, String link, Int32 length = 1024, Boolean forceReadMore = true)
        {
            link = $" [_Read More_]({link})";
            if (str.Length + link.Length > length || forceReadMore)
                return str.Truncate(length - link.Length) + link;
            return str;
        }
    }
}
