using System;
using System.Threading.Tasks;
using Denbot.Ingest.Models.Analytics;

namespace Denbot.Ingest.Services {
    public interface IAnalyticsService {
        /// <summary>
        /// Logs a message sent event to the analytics provider
        /// </summary>
        /// <param name="messageLog">Message log to be sent to the analytics provider</param>
        public Task LogMessageSentEventAsync(MessageLog messageLog);
        
        /// <summary>
        /// Logs a reaction added event to the analytics provider
        /// </summary>
        /// <param name="msgId">Discord ID of the message</param>
        /// <param name="userId">Discord ID of the user that added the reaction</param>
        /// <param name="addedAt">Timestamp of the reaction</param>
        /// <param name="emote">Emote string of the reaction</param>
        public Task LogReactionAddedEventAsync(ulong msgId, ulong userId, DateTimeOffset addedAt, string emote);

        /// <summary>
        /// Logs a voice activity event - user has joined, moved or left a voice channel
        /// </summary>
        /// <param name="voiceActivityLog">Log of the voice channel activity to be sent to the analytics provider
        /// </param>
        public Task LogVoiceActivityEventAsync(VoiceActivityLog voiceActivityLog);
    }
}