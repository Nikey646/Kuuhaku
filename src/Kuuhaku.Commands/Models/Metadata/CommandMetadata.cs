using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kuuhaku.Commands.Models.Metadata
{
    public class CommandMetadata : FallupMetadata
    {
        public String Name { get; set; }
        public String Command { get; set; }
        public String[] Aliases { get; set; }
        public String Summary { get; set; }
        public String Remarks { get; set; }
        public Int32 Priority { get; set; }

        public List<ArgumentMetadata> Arguments { get; set; } =
            new List<ArgumentMetadata>();

        [JsonIgnore]
        public ModuleMetadata Parent { get; internal set; } =new ModuleMetadata();
    }
}
