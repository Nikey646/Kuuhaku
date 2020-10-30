using System;

namespace Kuuhaku.BooruModule.Models
{
    public class BlacklistDto
    {
        public String Tag { get; }
        public String Reason { get; }

        public BlacklistDto(String tag, String reason)
        {
            this.Tag = tag;
            this.Reason = reason;
        }
    }
}
