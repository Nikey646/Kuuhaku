using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kuuhaku
{
    public class Program
    {
        public static async Task Main(String[] args)
        {
            var logger = Log.ForContext<Program>();

            try
            {
                using var host = CreateHostBuilder(args);

                    // TODO: Generate example config if it doesn't exist.

                logger.Information("Starting Kūhaku bot.");
                await host.StartAsync();

                logger.Information("Kūhaku startup has successfully been completed!");
                await host.WaitForShutdownAsync();
            }
            catch (Exception crap)
            {
                logger.Fatal(crap, "There was a fatal exception while the host was running.");
            }
            finally
            {
                Log.CloseAndFlush();
                if (logger is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static IHost CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder()
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


                    builder
                        .AddJsonFile(Path.Combine(ctx.HostingEnvironment.ContentRootPath, "Configs",
                        "Serilog.json"));
                    AddEnvConfigs(builder, "Kuuhaku", "Kūhaku");
                    AddFileConfigs(builder, "Kuuhaku", "Kūhaku");
                    builder.AddUserSecrets<Program>(true);
                })
                .UseStashbox(b =>
                    b.Configure(c =>
                        c.WithUnknownTypeResolution()
                            .WithDisposableTransientTracking()))
                .UseSerilog((ctx, b) => b.ReadFrom.Configuration(ctx.Configuration))
                .ConfigureDiscordHost<DiscordSocketClient>((ctx, b) =>
                {
                    // Allows running multiple versions of the bot on a single host, without having to use files to store the keys.
                    String tokenKey;
                    if ((tokenKey = ctx.Configuration["TokenKey"]) == null)
                        return;

                    b.Token = ctx.Configuration[tokenKey];
                    b.SocketConfig = new DiscordSocketConfig {LogLevel = LogSeverity.Verbose, MessageCacheSize = 200,};
                })
                .UseConsoleLifetime()
                .Build();
        }
    }
}
