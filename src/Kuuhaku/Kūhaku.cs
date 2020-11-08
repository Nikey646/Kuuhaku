using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Kuuhaku.Extensions;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Kuuhaku
{
    public class Kūhaku
    {
        private const String DefaultOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";

        public static async Task Main(String[] args)
        {
            // Create a logger until the host is built.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient.Discord.LogicalHandler", LogEventLevel.Information)
                .MinimumLevel.Override("System.Net.Http.HttpClient.Discord.ClientHandler", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: SystemConsoleTheme.Colored, outputTemplate: DefaultOutputTemplate,
                    applyThemeToRedirectedOutput: true)
                .CreateBootstrapLogger();

            try
            {
                Log.ForContext<Kūhaku>()
                    .Information("Building Host.");
                using var host = CreateHostBuilder(args)
                    .Build();

                Log.ForContext<Kūhaku>()
                    .Information("Starting Kūhaku Bot.");
                await host.StartAsync()
                    .ConfigureAwait(true);

                Log.ForContext<Kūhaku>()
                    .Information("Kūhaku startup has finished, waiting for shutdown");
                await host.WaitForShutdownAsync()
                    .ConfigureAwait(true);

                Log.ForContext<Kūhaku>()
                    .Information("Kūhaku has shut down.");
            }
            catch (Exception crap)
            {
                Log.ForContext<Kūhaku>().Fatal(crap, "There was an unexpected exception.");
                Debugger.Break();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(String[] args)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices((ctx, services) => { })
                .UseStashbox(b =>
                    b.Configure(c =>
                        c.WithUnknownTypeResolution()
                            .WithDisposableTransientTracking()))
                .UseSerilog((ctx, b) => b.ReadFrom.Configuration(ctx.Configuration))
                .UseDiscord(ctx =>
                {
                    String tokenKey;
                    if ((tokenKey = ctx.Configuration["TokenKey"]) == null)
                        throw new ArgumentException($"Unable to find \"tokenKey\" inside of the Configuration");

                    return ctx.Configuration[tokenKey] ??
                           throw new ArgumentException(
                               $"Unable to find {tokenKey.Quote()} inside of the Configuration");
                });

            return SystemdHelpers.IsSystemdService()
                ? builder.UseSystemd()
                : builder.UseConsoleLifetime();
        }

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
        {
            void AddFileConfigs(IConfigurationBuilder b, params String[] prefixes)
            {
                foreach (var prefix in prefixes)
                {
                    var configDir = Path.Combine(context.HostingEnvironment.ContentRootPath, "Configs");
                    var environment = context.HostingEnvironment.EnvironmentName;

                    b.AddJsonFile(Path.Combine(configDir, $"{prefix}.json"), true, true)
                        .AddJsonFile(Path.Combine(configDir, $"{prefix}.{environment}.json"), true, true);
                }
            }

            var chainSource = builder.Sources[0];
            builder.Sources.Clear();
            builder.Sources.Insert(0, chainSource);

            builder.AddEnvironmentVariables("Kūhaku:")
                .AddEnvironmentVariables("Kuuhaku:");

            AddFileConfigs(builder, "Kūhaku", "Kuuhaku", "Serilog");
        }
    }
}
