using System;

namespace Kuuhaku.Commands.Models.Metadata
{
    public class MetadataPath
    {
        public String CurrentModule { get; set; }
        public String CurrentCommand { get; set; }
        public Int32? CurrentArgument { get; set; }

        public MetadataPath(String moduleName)
        {
            this.CurrentModule = moduleName;
        }
    }
}
