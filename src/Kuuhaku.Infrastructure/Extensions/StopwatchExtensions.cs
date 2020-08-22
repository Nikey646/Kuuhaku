using System;
using System.Collections.Generic;
using Humanizer;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class StopwatchExtension
    {
        public static String ToDuration(this TimeSpan duration, Boolean includeMs = false)
        {
            var times = new List<String>();
            if (duration.TotalHours >= 24)
                times.Add("Day".ToQuantity(duration.Days));
            if (duration.Hours > 0)
                times.Add("Hour".ToQuantity(duration.Hours));
            if (duration.Minutes > 0)
                times.Add("Minute".ToQuantity(duration.Minutes));

            times.Add("Second".ToQuantity(duration.Seconds));
            if (includeMs)
                times.Add("Millisecond".ToQuantity(duration.Milliseconds));
            return times.Humanize();
        }
    }
}
