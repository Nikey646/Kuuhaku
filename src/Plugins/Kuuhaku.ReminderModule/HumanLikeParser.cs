using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using TimeSpanParserUtil;

namespace Kuuhaku.ReminderModule
{
    public static class HumanLikeParser
    {
        public static DateTime? Parse(String input)
        {
            return ParseWithSprintoParser(input);
        }

        private static DateTime? ParseWithMicrosoftRecongnizers(String input)
        {
            var parseResults = DateTimeRecognizer.RecognizeDateTime(input, Culture.English);
            if (parseResults.Count == 0)
                return default;


            var result = parseResults[0];
            var values = (IList<Dictionary<String, String>>) result.Resolution["values"];
            var subType = result.TypeName.Split('.').Last();

            switch (subType)
            {
                case "time":
                {
                    var value = values.Select(v => TimeSpan.Parse(v["value"])).FirstOrDefault();
                    if (value == default)
                        return default;

                    var now = DateTime.UtcNow;
                    return new DateTime(now.Year, now.Month, now.Day, value.Hours, value.Minutes, value.Seconds);
                }
                case "date":
                {
                    var value = values.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();
                    if (value == default)
                        return default;

                    var now = DateTime.UtcNow;
                    return value.Add(now.TimeOfDay);
                }
                case "datetime":
                {
                    var value = values.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();
                    return value == default ? default(DateTime?) : value;
                }
                default:
                    return default;
            }
        }

        private static DateTime? ParseWithSprintoParser(String input)
        {
            if (TimeSpanParser.TryParse(input, out var timeSpan))
                return DateTime.UtcNow.Add(timeSpan);
            return default;
        }


    }
}
