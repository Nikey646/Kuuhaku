using System;
using Newtonsoft.Json;

namespace Kuuhaku.Commands.Models.Metadata
{
    public class ArgumentMetadata
    {
        public String Summary { get; set; }

        [JsonIgnore]
        public Type TypeReader { get; set; }
        [JsonIgnore]
        public Boolean Remainder { get; set; }

        [JsonIgnore]
        public CommandMetadata Parent { get; internal set; } = new CommandMetadata();
}
}
