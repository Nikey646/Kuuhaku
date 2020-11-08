using System;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Kuuhaku.Models
{
    internal sealed class PluginsConfigurationOptions
    {
        public String Directory { get; set; }
        public Matcher FilePattern { get; }

        public PluginsConfigurationOptions()
        {
            this.FilePattern = new();
        }

        public PluginsConfigurationOptions WithDirectory(String directory)
        {
            if (directory.IsEmpty())
                throw new ArgumentException($"{nameof(directory)} should not be null or whitespace.", directory);
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
            this.Directory = directory;
            return this;
        }

        public PluginsConfigurationOptions WithFiles(params String[] patterns)
        {
            if (patterns.Length == 0)
                return this;

            foreach (var pattern in patterns)
            {
                if (pattern.IsEmpty())
                    continue;

                this.FilePattern.AddInclude(pattern);
            }

            return this;
        }

        public PluginsConfigurationOptions WithoutFiles(params String[] patterns)
        {
            if (patterns.Length == 0)
                return this;

            foreach (var pattern in patterns)
            {
                if (pattern.IsEmpty())
                    continue;

                this.FilePattern.AddExclude(pattern);
            }

            return this;
        }

    }
}
