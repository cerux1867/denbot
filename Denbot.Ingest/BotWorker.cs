using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Denbot.Ingest.Commands;
using Denbot.Ingest.InteractionHandlers;
using Denbot.Ingest.Models;
using Denbot.Ingest.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Denbot.Ingest {
    public class BotWorker : BackgroundService {
        private readonly DiscordClient _discordClient;
        private readonly IAnalyticsService _analyticsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly InteractionResolver _interactionResolver;
        private readonly ILogger<BotWorker> _logger;
        private readonly IOptions<DiscordSettings> _options;
        private const string EmotePattern = @"<(:.+?:\d+)>";

        public BotWorker(DiscordClient client, IAnalyticsService analyticsService, IServiceProvider serviceProvider,
            InteractionResolver interactionResolver, ILogger<BotWorker> logger, IOptions<DiscordSettings> options) {
            _discordClient = client;
            _analyticsService = analyticsService;
            _serviceProvider = serviceProvider;
            _interactionResolver = interactionResolver;
            _logger = logger;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _discordClient.MessageCreated += async (_, args) => {
                if (args.Author.IsBot) return;

                var emotes = Regex.Matches(args.Message.Content, EmotePattern)
                    .Select(m => m.ToString()).Distinct();

                var attachmentMimeTypes = args.Message.Attachments.Select(a => a.MediaType)
                    .GroupBy(a => a).Select(a => a.Key).ToList();

                await _analyticsService.LogMessageSentEventAsync(args.Message.Id, args.Author.Id, args.Channel.Id,
                    args.Guild.Id, args.Message.Content, args.Message.Timestamp, emotes.ToArray(), 
                    args.Message.MentionedUsers.Where(u => u.IsBot == false).Select(u => u.Id).ToArray(),
                    args.Message.MentionedRoles.Select(r => r.Id).ToArray(), 
                    args.MentionedChannels.Select(u => u.Id).ToArray(), 
                    args.Message.MentionEveryone , attachmentMimeTypes.ToArray(),
                    args.Message.Reference?.Message.Id, args.Message.Thread?.Id);
            };

            _discordClient.MessageReactionAdded += async (_, args) => {
                await _analyticsService.LogReactionAddedEventAsync(args.Message.Id, args.User.Id, DateTimeOffset.Now,
                    args.Emoji.ToString());
            };
            
            _discordClient.ComponentInteractionCreated += async (_, args) => {
                _logger.LogInformation(
                    "Component interaction {ComponentId} created by user {DiscordUserId} in guild '{DiscordGuildId}' with values {ComponentValues}",
                    args.Id, args.User.Id, args.Guild.Id, args.Values);
                try {
                    await _interactionResolver.ResolveInteractionAsync(args.Interaction);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error occured while handling component interaction {ComponentId}", args.Id);
                    throw;
                }
            };
            _discordClient.ContextMenuInteractionCreated += (_, args) => {
                _logger.LogInformation(
                    "Context menu interaction {InteractionId} created by user {DiscordUserId} in guild '{DiscordGuildId}'",
                    args.Interaction.Id, args.Interaction.User.Id, args.Interaction.Guild.Id);
                return Task.CompletedTask;
            };
            _discordClient.InteractionCreated += (_, args) => {
                _logger.LogInformation(
                    "Interaction {InteractionId} created by user {DiscordUserId} in guild '{DiscordGuildId}'",
                    args.Interaction.Id, args.Interaction.User.Id, args.Interaction.Guild.Id);
                return Task.CompletedTask;
            };
            var applicationCommands = _discordClient.UseApplicationCommands(new ApplicationCommandsConfiguration {
                Services = _serviceProvider
            });
            applicationCommands.SlashCommandErrored += (_, args) => {
                _logger.LogError(args.Exception,
                    "Error occured while executing slash command '{CommandName}' for interaction {InteractionId}",
                    args.Context.CommandName, args.Context.InteractionId);
                return Task.CompletedTask;
            };
            applicationCommands.ContextMenuErrored += (_, args) => {
                _logger.LogError(args.Exception,
                    "Error occured while executing context menu command '{CommandName}' for interaction {InteractionId}",
                    args.Context.CommandName, args.Context.InteractionId);
                return Task.CompletedTask;
            };

            foreach (var serverId in _options.Value.SlashCommandServers) {
                applicationCommands.RegisterCommands<SlashUnhomieModule>(serverId);
                applicationCommands.RegisterCommands<ContextMenuUnhomieModule>(serverId);
            }

            await _discordClient.ConnectAsync();
            _logger.LogInformation("Denbot ingest system started");
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            await _discordClient.DisconnectAsync();
            _logger.LogInformation("Denbot ingest system stopped");
            await base.StopAsync(cancellationToken);
        }
    }
}