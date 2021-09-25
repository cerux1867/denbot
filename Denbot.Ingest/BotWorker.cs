using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Denbot.Ingest.Commands;
using Denbot.Ingest.InteractionHandlers;
using Denbot.Ingest.Models;
using Denbot.Ingest.Models.Analytics;
using Denbot.Ingest.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Denbot.Ingest {
    public class BotWorker : BackgroundService {
        private readonly DiscordClient _discordClient;
        private readonly IAnalyticsService _analyticsService;
        private readonly IAnalyticsCorrelationService _analyticsCorrelationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly InteractionResolver _interactionResolver;
        private readonly ILogger<BotWorker> _logger;
        private readonly IOptions<DiscordSettings> _options;
        private const string EmotePattern = @"<(:.+?:\d+)>";

        public BotWorker(DiscordClient client, IAnalyticsService analyticsService, IAnalyticsCorrelationService analyticsCorrelationService, IServiceProvider serviceProvider,
            InteractionResolver interactionResolver, ILogger<BotWorker> logger, IOptions<DiscordSettings> options) {
            _discordClient = client;
            _analyticsService = analyticsService;
            _analyticsCorrelationService = analyticsCorrelationService;
            _serviceProvider = serviceProvider;
            _interactionResolver = interactionResolver;
            _logger = logger;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _discordClient.MessageCreated += (_, args) => {
                Task.Run(async () => {
                    if (args.Author.IsBot) return;
                    try {
                        await _analyticsService.LogMessageSentEventAsync(ConstructMessageLogFromMessage(args.Message,
                            args.MentionedChannels, args.MentionedRoles, args.MentionedUsers));
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error occured while attempting to process an analytics event {EventType} with data {MessageId}", "MessageCreated", args.Message.Id);
                    }
                }, stoppingToken);
                return Task.CompletedTask;
            };

            _discordClient.MessageUpdated += (_, args) => {
                Task.Run(async () => {
                    if (args.Author.IsBot) return;
                    try {
                        await _analyticsCorrelationService.UpdateMessageIfExistsAsync(
                            ConstructMessageLogFromMessage(args.Message, args.MentionedChannels, args.MentionedRoles,
                                args.MentionedUsers));
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error occured while attempting to correlate an analytics event {EventType} with data {MessageId}", "MessageUpdated", args.Message.Id);
                    }

                }, stoppingToken);
                return Task.CompletedTask;
            };

            _discordClient.MessageDeleted += (_, args) => {
                Task.Run(async () => {
                    try {
                        await _analyticsCorrelationService.DeleteMessageIfExistsAsync(args.Message.Id);
                    } catch (Exception ex) {
                        _logger.LogError(ex, "Error occured while attempting to correlate an analytics event {EventType} with data {MessageId}", "DeleteMessage", args.Message.Id);
                    }
                }, stoppingToken);
                return Task.CompletedTask;
            };

            _discordClient.MessageReactionAdded += (_, args) => {
                Task.Run(async () => {
                    try {
                        await _analyticsService.LogReactionAddedEventAsync(args.Message.Id, args.User.Id,
                            DateTimeOffset.Now,
                            args.Emoji.ToString());
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error occured while attempt to process an analytics event {EventType} with data {MessageId}, {UserId}", "ReactionAdded", args.Message.Id, args.User.Id);
                    }
                }, stoppingToken);
                return Task.CompletedTask;
            };

            _discordClient.MessageReactionRemoved += (_, args) => {
                Task.Run(async () => {
                    try {
                        await _analyticsCorrelationService.DeleteReactionIfExistsAsync(args.Message.Id, args.User.Id);
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error occured while attempting to correlate an analytics event {EventType} with data {MessageId}, {UserId}", "ReactionRemoved", args.Message.Id, args.User.Id);
                    }
                }, stoppingToken);
                return Task.CompletedTask;
            };
            
            _discordClient.ComponentInteractionCreated += (_, args) => {
                Task.Run(async () => {
                    _logger.LogInformation(
                        "Component interaction {ComponentId} created by user {DiscordUserId} in guild '{DiscordGuildId}' with values {ComponentValues}",
                        args.Id, args.User.Id, args.Guild.Id, args.Values);
                    try {
                        await _interactionResolver.ResolveInteractionAsync(args.Interaction);
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error occured while handling component interaction {ComponentId}",
                            args.Id);
                        throw;
                    }
                }, stoppingToken);
                return Task.CompletedTask;
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

        private MessageLog ConstructMessageLogFromMessage(DiscordMessage msg, IEnumerable<DiscordChannel> mentionedChannels, 
            IEnumerable<DiscordRole> mentionedRoles, IEnumerable<DiscordUser> mentionedUsers) {
            var emotes = Regex.Matches(msg.Content, EmotePattern)
                .Select(m => m.ToString()).Distinct();

            var attachmentMimeTypes = msg.Attachments.Select(a => a.MediaType)
                .GroupBy(a => a).Select(a => a.Key).ToList();

            return new MessageLog {
                MessageId = msg.Id,
                ChannelId = msg.ChannelId,
                GuildId = msg.Channel.Guild.Id,
                Emotes = emotes.ToArray(),
                AttachmentMimeTypes = attachmentMimeTypes.ToArray(),
                Message = msg.Content,
                Timestamp = msg.Timestamp,
                UserMentions = mentionedUsers.Where(u => u.IsBot == false).Select(u => u.Id)
                    .ToArray(),
                RoleMentions = mentionedRoles.Select(r => r.Id).ToArray(),
                ChannelMentions = mentionedChannels.Select(r => r.Id).ToArray(),
                MentionsEveryone = msg.MentionEveryone,
                ThreadId = msg.Thread?.Id,
                UserId = msg.Author.Id,
                RepliedMessageId = msg.Reference?.Message.Id
            };
        }
    }
}