using System;
using System.Linq;
using System.Threading.Tasks;
using Denbot.Ingest.Attributes;
using Denbot.Ingest.Models;
using DSharpPlusNextGen;
using DSharpPlusNextGen.Entities;
using DSharpPlusNextGen.SlashCommands;

namespace Denbot.Ingest.Commands {
    public class DevModule : SlashCommandModule {
        private readonly IngestStatus _ingestStatus;

        public DevModule(IngestStatus ingestStatus) {
            _ingestStatus = ingestStatus;
        }
        
        [SlashIsOwnerOrRequireUserPermission(Permissions.Administrator)]
        [SlashCommand("info", "Gets ingest system information")]
        public async Task InfoAsync(InteractionContext context) {
            if (context.Client.CurrentApplication.Owners.FirstOrDefault(o => o.Id == context.User.Id) == null) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "You are not the bot owner or bot developer and cannot use this command").AsEphemeral(true));
                return;
            }

            var startupTime = _ingestStatus.StartedAt.HasValue ? _ingestStatus.StartedAt.Value.ToString() : "Unknown";
            var lastProcessedTime = _ingestStatus.LastInteractionProcessedAt.HasValue
                ? _ingestStatus.LastInteractionProcessedAt.Value.ToString()
                : "No interactions since startup";

            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor("#fff203"))
                .WithTitle("Ingest System Information")
                .WithTimestamp(DateTime.Now)
                .AddField("API Latency", $"{context.Client.Ping}ms", true)
                .AddField("Gateway URL", $"{context.Client.GatewayInfo.Url}", true)
                .AddField("Gateway Version", $"{context.Client.GatewayVersion}", true)
                .AddField("DSharpPlusNextGen Version", $"{context.Client.VersionString}", true)
                .AddField("Ingest Started At", startupTime, true)
                .AddField("Processed Interactions", _ingestStatus.NumProcessedInteractions.ToString(), true)
                .AddField("Last Processed Interaction At", lastProcessedTime, true)
                .AddField("Connected Guilds", $"{string.Join(",", context.Client.Guilds.Values.Select(g => $"{g.Name} ({g.Id})"))}");
            var interaction = new DiscordInteractionResponseBuilder()
                .AddEmbed(embed)
                .AsEphemeral(true);
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, interaction);
        }
    }
}