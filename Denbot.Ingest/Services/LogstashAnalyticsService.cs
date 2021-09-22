using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Denbot.Ingest.Models;
using Denbot.Ingest.Models.Analytics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Denbot.Ingest.Services {
    public class LogstashAnalyticsService : IAnalyticsService {
        private readonly IOptions<AnalyticsSettings> _settings;
        private readonly ILogger<LogstashAnalyticsService> _logger;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public LogstashAnalyticsService(IOptions<AnalyticsSettings> settings, ILogger<LogstashAnalyticsService> logger, HttpClient httpClient) {
            _settings = settings;
            _logger = logger;
            _httpClient = httpClient;
            _jsonSerializerOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        
        /// <inheritdoc />
        public async Task LogMessageSentEventAsync(ulong msgId, ulong authorId, ulong channelId, ulong guildId, string message, DateTimeOffset sentAt,
            string[] emotes, ulong[] userMentions, ulong[] roleMentions, ulong[] channelMentions, bool mentionsEveryone, 
            string[] attachmentMimeTypes, ulong? repliesTo, ulong? threadId) {
            if (ShouldBeIgnored(guildId, channelId)) {
                return;
            }
            var msgLog = new MessageLog {
                MessageId = msgId,
                UserId = authorId,
                ChannelId = channelId,
                GuildId = guildId,
                Message = message,
                Timestamp = sentAt,
                Emotes = emotes,
                UserMentions = userMentions,
                RoleMentions = roleMentions,
                ChannelMentions = channelMentions,
                MentionsEveryone = mentionsEveryone,
                AttachmentMimeTypes = attachmentMimeTypes,
                RepliedMessageId = repliesTo.GetValueOrDefault(),
                ThreadId = threadId.GetValueOrDefault()
            };
            var response = await _httpClient.PutAsync($"{_settings.Value.LogstashUrl}", 
                new StringContent(JsonSerializer.Serialize(msgLog, _jsonSerializerOptions), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Could not dispatch message analytics event with ID {MessageId}. Received response status code {StatusCode} with response body {ResponseBody}",
                    msgId, response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        /// <inheritdoc />
        public async Task LogReactionAddedEventAsync(ulong msgId, ulong userId, DateTimeOffset addedAt, string emote) {
            var reactLog = new ReactionLog {
                Emote = emote,
                Timestamp = addedAt,
                MessageId = msgId,
                UserId = userId
            };
            var response = await _httpClient.PutAsync($"{_settings.Value.LogstashUrl}", 
                new StringContent(JsonSerializer.Serialize(reactLog, _jsonSerializerOptions), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Could not dispatch reaction analytics event with ID {MessageId}. Received response status code {StatusCode} with response body {ResponseBody}",
                    msgId, response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        private bool ShouldBeIgnored(ulong guildId, ulong channelId) {
            if (_settings.Value.IgnoreDict.ContainsKey(guildId.ToString())) {
                _settings.Value.IgnoreDict.TryGetValue(guildId.ToString(), out var channels);
                if (channels == null) {
                    return true;
                }

                if (channels.Length > 0) {
                    if (channels.Contains(channelId)) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}