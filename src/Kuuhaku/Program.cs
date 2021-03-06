﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Kuuhaku.Commands;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Serilog;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace Kuuhaku
{
    public class Program
    {
        public static async Task Main(String[] args)
        {
            try
            {
                using var host = CreateHostBuilder(args).Build();

                // TODO: Generate example config if it doesn't exist.

                Log.ForContext<Program>().Information("Starting Kūhaku bot.");
                await host.StartAsync();

                Log.ForContext<Program>().Information("Kūhaku startup has successfully been completed!");

                await host.WaitForShutdownAsync();
            }
            catch (Exception crap)
            {
                Log.ForContext<Program>().Fatal(crap, "There was a fatal exception while the host was running.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    void AddEnvConfigs(IConfigurationBuilder b, params String[] prefixes)
                    {
                        foreach (var prefix in prefixes)
                        {
                            b.AddEnvironmentVariables($"{prefix}_")
                                .AddEnvironmentVariables($"{prefix}:");
                        }
                    }

                    void AddFileConfigs(IConfigurationBuilder b, params String[] prefixes)
                    {
                        foreach (var prefix in prefixes)
                        {
                            var configDir = Path.Combine(ctx.HostingEnvironment.ContentRootPath, "Configs");

                            // Make configs directory a catch all? Include all of the files in there, instead of just the prefix'd ones?
                            b.AddJsonFile(Path.Combine(configDir, $"{prefix}.json"), true, true)
                                .AddJsonFile(Path.Combine(configDir,
                                    $"{prefix}.{ctx.HostingEnvironment.EnvironmentName}.json"), true, true);
                        }
                    }

                    // Remove the default appsettings.json and environment sources.
                    // Keep the chain source though.
                    var chainSource = builder.Sources[0];
                    builder.Sources.Clear();
                    builder.Sources.Insert(0, chainSource);

                    builder.AddUserSecrets<Program>(false);
                    builder
                        .AddJsonFile(Path.Combine(ctx.HostingEnvironment.ContentRootPath, "Configs",
                            "Serilog.json"));
                    AddEnvConfigs(builder, "Kuuhaku", "Kūhaku");
                    AddFileConfigs(builder, "Kuuhaku", "Kūhaku");
                })
                .ConfigureServices((ctx, services) =>
                {
                    var commandsFactory = new KuuhakuCommandsFactory();
                    commandsFactory.ConfigureServices(ctx, services);
                    services.AddSingleton<IPluginFactory>(commandsFactory);

                    // TODO: Use information from configuration file
                    services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(new RedisConfiguration
                    {
                        Ssl = false,
                        Hosts = new []
                        {
                            new RedisHost { Host = "localhost", Port = 6379 },
                        },
                        KeyPrefix = "Kuuhaku__",
                        Database = 0,
                    });
                })
                .UseStashbox(b =>
                    b.Configure(c =>
                        c.WithUnknownTypeResolution()
                            .WithDisposableTransientTracking()))
                .UseSerilog((ctx, b) => b.ReadFrom.Configuration(ctx.Configuration))
                .UsePlugins((ctx, b) =>
                    b.WithDirectory(Path.Combine(ctx.HostingEnvironment.ContentRootPath, "Plugins"))
                        .WithFilePattern("Kuuhaku.*.dll")
                        .WithFilePattern("Kūhaku.*.dll"))
                .ConfigureDiscordHost<DiscordSocketClient>((ctx, b) =>
                {
                    // Allows running multiple versions of the bot on a single host, without having to use files to store the keys.
                    String tokenKey;
                    if ((tokenKey = ctx.Configuration["TokenKey"]) == null)
                        return;

                    b.Token = ctx.Configuration[tokenKey];
                    b.SocketConfig = new DiscordSocketConfig {LogLevel = LogSeverity.Verbose, MessageCacheSize = 200,};
                });

            if (SystemdHelpers.IsSystemdService())
                return builder.UseSystemd();

            return builder
                .UseConsoleLifetime();
        }
    }
}
