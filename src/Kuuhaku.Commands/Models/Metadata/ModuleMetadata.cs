using System;
using System.Collections.Generic;

namespace Kuuhaku.Commands.Models.Metadata
{
    public class ModuleMetadata : FallupMetadata
    {
        public String Name { get; set; }
        public String Group { get; set; }
        public String[] Aliases { get; set; }
        public String Summary { get; set; }
        public String Remarks { get; set; }

        public Dictionary<String, CommandMetadata> Commands { get; set; } = new Dictionary<String, CommandMetadata>();
    }
}
