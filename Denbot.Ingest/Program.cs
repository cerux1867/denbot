using System;
using System.IO;
using Denbot.Ingest.InteractionHandlers;
using Denbot.Ingest.Models;
using Denbot.Ingest.Services;
using DSharpPlusNextGen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using Serilog.Extensions.Logging;

namespace Denbot.Ingest {
    public class Program {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
                true, true)
            .AddEnvironmentVariables()
            .Build();

        public static void Main(string[] args) {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) => {
                    services.Configure<DiscordSettings>(hostContext.Configuration.GetSection("Discord"));
                    services.Configure<BackendSettings>(hostContext.Configuration.GetSection("Backend"));
                    services.AddQuartz(q => { q.UseMicrosoftDependencyInjectionJobFactory(); });
                    services.AddQuartzHostedService(q => { q.WaitForJobsToComplete = true; });
                    services.AddHttpClient<IRemoveRoleVoteService, RemoveRoleVoteHttpService>();
                    services.AddSingleton(provider => {
                        var settings = provider.GetRequiredService<IOptions<DiscordSettings>>();
                        return new DiscordClient(new DiscordConfiguration {
                            Token = settings.Value.Token,
                            TokenType = TokenType.Bot,
                            LoggerFactory = new SerilogLoggerFactory(Log.Logger)
                        });
                    });
                    services.AddSingleton<RoleRemovalBallotHandler>();
                    services.AddSingleton<InteractionResolver>();
                    services.AddSingleton<IngestStatus>();
                    services.AddHostedService<BotWorker>();
                });
    }
}