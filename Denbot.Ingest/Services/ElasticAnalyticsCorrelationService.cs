using System.Linq;
using System.Threading.Tasks;
using Denbot.Ingest.Models;
using Denbot.Ingest.Models.Analytics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Denbot.Ingest.Services {
    public class ElasticAnalyticsCorrelationService : IAnalyticsCorrelationService {
        private readonly ILogger<ElasticAnalyticsCorrelationService> _logger;
        private readonly IElasticClient _elasticClient;
        private readonly IOptions<AnalyticsSettings> _settings;

        public ElasticAnalyticsCorrelationService(ILogger<ElasticAnalyticsCorrelationService> logger,
            IElasticClient elasticClient, IOptions<AnalyticsSettings> settings) {
            _logger = logger;
            _elasticClient = elasticClient;
            _settings = settings;
        }

        public async Task DeleteMessageIfExistsAsync(ulong messageId) {
            var response = await _elasticClient.DeleteByQueryAsync<MessageLog>(d => d
                .Index(_settings.Value.AnalyticsIndexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                                .Match(e => e
                                    .Field(l => l.MessageId).Query(messageId.ToString())),
                            m => m.Match(e => e
                                .Field(l => l.EventType)
                                .Query(((int)AnalyticsEventType.Message).ToString()))
                        )
                    )
                )
            );
            if (!response.IsValid) {
                _logger.LogError("Unable to correlate message deletion of Discord message with ID {DiscordMessageId}",
                    messageId);
            }
        }

        public async Task UpdateMessageIfExistsAsync(MessageLog msg) {
            var mentionsEveryone = msg.MentionsEveryone ? "true" : "false";
            var response = await _elasticClient.UpdateByQueryAsync<MessageLog>(u => u
                .Index(_settings.Value.AnalyticsIndexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                                .Match(f => f
                                    .Field(l => l.MessageId).Query(msg.MessageId.ToString())),
                            m => m
                                .Match(f => f
                                    .Field(l => l.EventType).Query(((int)msg.EventType).ToString()))
                        )
                    )
                )
                .Script(s => s
                    .Source($@"
                    ctx._source.mentionsEveryone = {mentionsEveryone}; 
                    ctx._source.message = '{msg.Message}'; 
                    ctx._source.userMentions = [{string.Join(',', msg.UserMentions.Select(i => $"{i}L"))}];
                    ctx._source.roleMentions = [{string.Join(',', msg.RoleMentions.Select(i => $"{i}L"))}];
                    ctx._source.channelMentions = [{string.Join(',', msg.ChannelMentions.Select(i => $"{i}L"))}];
                    ctx._source.emotes = [{string.Join(',', msg.Emotes.Select(i => $"'{i}'"))}];
                    ctx._source.attachmentMimeTypes = [{string.Join(',', msg.AttachmentMimeTypes.Select(i => $"'{i}'"))}];"
                    )
                )
            );

            if (!response.IsValid) {
                _logger.LogError("Unable to correlate message update of Discord message with ID {DiscordMessageId}",
                    msg.MessageId);
            }
        }

        public async Task DeleteReactionIfExistsAsync(ulong msgId, ulong userId) {
            var response = await _elasticClient.DeleteByQueryAsync<ReactionLog>(d => d
                .Index(_settings.Value.AnalyticsIndexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                                .Match(e => e
                                    .Field(l => l.MessageId).Query(msgId.ToString())),
                            m => m.Match(e => e
                                .Field(l => l.EventType)
                                .Query(((int)AnalyticsEventType.Reaction).ToString())),
                            m => m.Match(e =>
                                e.Field(l => l.UserId).Query(userId.ToString()))
                        )
                    )
                )
            );
            if (!response.IsValid) {
                _logger.LogError("Unable to correlate reaction removal on Discord message with ID {DiscordMessageId} from user {UserId}", msgId, userId);
            }
        }
    }
}