using System;
using System.Threading;
using System.Threading.Tasks;
using Denbot.Ingest.Commands;
using Denbot.Ingest.InteractionHandlers;
using Denbot.Ingest.Models;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Denbot.Ingest {
    public class BotWorker : BackgroundService {
        private readonly DiscordClient _discordClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IngestStatus _ingestStatus;
        private readonly InteractionResolver _interactionResolver;
        private readonly ILogger<BotWorker> _logger;
        private readonly IOptions<DiscordSettings> _options;

        public BotWorker(DiscordClient client, IServiceProvider serviceProvider, IngestStatus ingestStatus, InteractionResolver interactionResolver, ILogger<BotWorker> logger, IOptions<DiscordSettings> options) {
            _discordClient = client;
            _serviceProvider = serviceProvider;
            _ingestStatus = ingestStatus;
            _interactionResolver = interactionResolver;
            _logger = logger;
            _options = options;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _discordClient.ComponentInteractionCreated += async (_, args) => {
                _ingestStatus.LastInteractionProcessedAt = DateTimeOffset.Now;
                _ingestStatus.NumProcessedInteractions++;
                _logger.LogInformation("Component interaction created");
                await _interactionResolver.ResolveInteractionAsync(args.Interaction);
            };
            _discordClient.InteractionCreated += (_, _) => {
                _ingestStatus.LastInteractionProcessedAt = DateTimeOffset.Now;
                _ingestStatus.NumProcessedInteractions++;
                _logger.LogInformation("Interaction created");
                return Task.CompletedTask;
            };
            var slashCommands = _discordClient.UseApplicationCommands(new ApplicationCommandsConfiguration {
                Services = _serviceProvider
            });
            foreach (var serverId in _options.Value.SlashCommandServers) {
                slashCommands.RegisterCommands<DevModule>(serverId);
                slashCommands.RegisterCommands<UnhomieModule>(serverId);
            }

            await _discordClient.ConnectAsync();
            _ingestStatus.StartedAt = DateTimeOffset.Now;
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            await _discordClient.DisconnectAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}