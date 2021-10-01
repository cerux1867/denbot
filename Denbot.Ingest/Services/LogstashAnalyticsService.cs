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

            _logger.LogInformation("Analytics enabled: {AnalyticsEnabled}", _settings.Value.Enabled);
        }
        
        /// <inheritdoc />
        public async Task LogMessageSentEventAsync(MessageLog messageLog) {
            if (ShouldBeIgnored(messageLog.GuildId, messageLog.ChannelId)) {
                return;
            }

            var response = await _httpClient.PutAsync($"{_settings.Value.LogstashUrl}",
                new StringContent(JsonSerializer.Serialize(messageLog, _jsonSerializerOptions), Encoding.UTF8,
                    "application/json"));
            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Could not dispatch message analytics event with ID {MessageId}. Received response status code {StatusCode} with response body {ResponseBody}",
                    messageLog.MessageId, response.StatusCode, await response.Content.ReadAsStringAsync());
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
                new StringContent(JsonSerializer.Serialize(reactLog, _jsonSerializerOptions), Encoding.UTF8,
                    "application/json"));
            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Could not dispatch reaction analytics event with ID {MessageId}. Received response status code {StatusCode} with response body {ResponseBody}",
                    msgId, response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        /// <inheritdoc />
        public async Task LogVoiceActivityEventAsync(VoiceActivityLog voiceActivityLog) {
            if (!_settings.Value.Enabled) {
                return;
            }

            var response = await _httpClient.PutAsync($"{_settings.Value.LogstashUrl}",
                new StringContent(JsonSerializer.Serialize(voiceActivityLog, _jsonSerializerOptions), Encoding.UTF8,
                    "application/json"));
            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Could not dispatch voice activity analytics event with ID in channel {ChannelId}. Received response status code {StatusCode} with response body {ResponseBody}",
                    voiceActivityLog.VoiceChannelId, response.StatusCode, 
                    await response.Content.ReadAsStringAsync());
            }
        }

        private bool ShouldBeIgnored(ulong guildId, ulong channelId) {
            if (!_settings.Value.Enabled) {
                return true;
            }
            
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