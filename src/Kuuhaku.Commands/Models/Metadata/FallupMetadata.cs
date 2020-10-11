using System;
using Discord.Commands;

namespace Kuuhaku.Commands.Models.Metadata
{
    public class FallupMetadata
    {
        public ContextType ContextType { get; set; }
        // public Permissions MinimumPermission { get; set; } // TODO
        // public UInt32 RateLimit { get; set; } // TODO
        // public TimeSpan RateLimitTimeout { get; set; }
    }
}
