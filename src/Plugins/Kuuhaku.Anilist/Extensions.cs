using System;
using System.Collections.Generic;
using Humanizer;
using Kuuhaku.Anilist.Models;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Anilist
{
    public static class Extensions
    {

        public static String GetTitle(this AnilistSeries series)
        {
            if (!series.Title.English.IsEmpty())
                return series.Title.English;
            if (!series.Title.Romaji.IsEmpty())
                return series.Title.Romaji;
            return series.Title.Native;
        }

        public static String GetDescription(this AnilistSeries series, Int32 length = 512, Boolean force = false)
        {
            return (series.Description ?? "N/A")
                .Replace("\n", "")
                .Replace("<br>", "\n")
                .ReadMore(series.SiteUrl, length, force);
        }

        public static String GetDate(this AnilistFuzzyDate date)
        {
            var nums = new List<Int32>();
            if (date.Day.HasValue)
                nums.Add(date.Day.Value);
            if (date.Month.HasValue)
                nums.Add(date.Month.Value);
            if (date.Year.HasValue)
                nums.Add(date.Year.Value);

            return String.Join("/", nums);
        }

        public static String GetCover(this AnilistSeries series)
        {
            if (series.CoverImage.ExtraLarge.IsEmpty())
                return series.CoverImage.Medium;
            return series.CoverImage.ExtraLarge;
        }

        public static String GetAsString(this IEnumerable<String> strings, Int32 maxLength = 1023)
        {
            const String more = "More…";
            var length = more.Length;

            var words = new List<String>();

            foreach (var s in strings)
            {
                if (s.Length + length > maxLength)
                {
                    words.Add(more);
                    break;
                }

                words.Add(s);
                length += s.Length;
            }
            return words.Humanize();
        }

    }
}
