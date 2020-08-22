using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kuuhaku.Infrastructure.Interfaces;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku
{
    public static class HostingExtensions
    {
        public static IHostBuilder UsePlugins(this IHostBuilder b,
            Func<HostBuilderContext, PluginsConfigurationOptions, PluginsConfigurationOptions> optsBuilder)
        {
            var loaders = new List<PluginLoader>();
            return b.ConfigureServices((ctx, services) =>
            {
                var opts = optsBuilder(ctx, new PluginsConfigurationOptions());
                if (String.IsNullOrWhiteSpace(opts.Directory))
                    throw new ArgumentException($"Invalid {nameof(PluginsConfigurationOptions.Directory)} provided",
                        nameof(PluginsConfigurationOptions.Directory));

                var pluginPaths = opts.FilePattern.GetResultsInFullPath(opts.Directory);
                foreach (var pluginPath in pluginPaths)
                {
                    if (!File.Exists(pluginPath))
                        continue; // Sanity check, but why did it even match...?

                    var loader = PluginLoader.CreateFromAssemblyFile(pluginPath, c => c.PreferSharedTypes = true);
                    loaders.Add(loader);
                    services.AddSingleton(loader);
                }

                // Collapses to a final foreach that is too complex to maintain
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var loader in loaders)
                {
                    var assembly = loader.LoadDefaultAssembly();
                    foreach (var factoryType in assembly.GetTypes().Where(t => typeof(IPluginFactory).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
                    {
                        var factory = (IPluginFactory) Activator.CreateInstance(factoryType);
                        factory?.ConfigureServices(ctx, services);
                        services.AddSingleton(factory);
                    }
                }
            });
        }
    }

    public class PluginsConfigurationOptions
    {
        public String Directory { get; set; }
        public Matcher FilePattern { get; }

        public PluginsConfigurationOptions()
        {
            this.FilePattern = new Matcher();
        }

        public PluginsConfigurationOptions WithDirectory(String directory)
        {
            this.Directory = directory;
            return this;
        }

        public PluginsConfigurationOptions WithFilePattern(String pattern)
        {
            this.FilePattern.AddInclude(pattern);
            return this;
        }
    }
}
