using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.Models;
using Kuuhaku.Services;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Gateway.Extensions;

namespace Kuuhaku.Extensions
{
    public static class HostingExtensions
    {
        public static IHostBuilder UseDiscord(this IHostBuilder builder, Func<HostBuilderContext, String> tokenFunc)
        {
            return builder.ConfigureServices((ctx, services) => services
                .AddHostedServiceSingleton<DiscordService>()
                .AddDiscordGateway(() => tokenFunc(ctx)));
        }

        internal static IHostBuilder UsePlugins(this IHostBuilder builder,
            Func<HostBuilderContext, PluginsConfigurationOptions, PluginsConfigurationOptions> optsBuilder)
        {
            if (optsBuilder == null) throw new ArgumentNullException(nameof(optsBuilder));

            var loaders = new List<PluginLoader>();
            return builder.ConfigureServices((ctx, services) =>
            {
                var opts = optsBuilder(ctx, new PluginsConfigurationOptions());
                if (opts.Directory.IsEmpty())
                    throw new ArgumentException($"Invalid {nameof(PluginsConfigurationOptions.Directory)} provided.",
                        nameof(PluginsConfigurationOptions.Directory));

                var pluginPaths = opts.FilePattern.GetResultsInFullPath(opts.Directory);

                foreach (var pluginPath in pluginPaths)
                {
                    if (!File.Exists(pluginPath))
                        continue;

                    var loader = PluginLoader.CreateFromAssemblyFile(pluginPath, c =>
                    {
                        c.PreferSharedTypes = c.LoadInMemory = true;
                        c.EnableHotReload = c.IsUnloadable = false;
                    });
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
}
