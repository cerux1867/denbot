using System;
using System.Threading.Tasks;

namespace Denbot.Ingest.Services {
    public interface IAnalyticsService {
        /// <summary>
        /// Logs a message sent event to the analytics provider
        /// </summary>
        /// <param name="msgId">Discord ID of the message</param>
        /// <param name="authorId">Discord ID of the message author</param>
        /// <param name="channelId">Discord ID of the channel the message was sent in</param>
        /// <param name="guildId">Discord ID of the guild the message was sent in</param>
        /// <param name="sentAt">Timestamp of the message</param>
        /// <param name="emotes">Array of emote strings</param>
        /// <param name="userMentions">Users this message mentions, if any</param>
        /// <param name="roleMentions">Roles this message mentions, if any - including @everyone</param>
        /// <param name="attachmentMimeTypes">Mime types of the attached files</param>
        /// <param name="repliesTo">Discord ID of the message this message replies to</param>
        /// <param name="threadId">Discord ID of the thread this message is in</param>
        public Task LogMessageSentEventAsync(ulong msgId, ulong authorId, ulong channelId, ulong guildId, DateTimeOffset sentAt,
            string[] emotes, ulong[] userMentions, ulong[] roleMentions, string[] attachmentMimeTypes, ulong? repliesTo, 
            ulong? threadId);
        
        /// <summary>
        /// Logs a reaction added event to the analytics provider
        /// </summary>
        /// <param name="msgId">Discord ID of the message</param>
        /// <param name="userId">Discord ID of the user that added the reaction</param>
        /// <param name="addedAt">Timestamp of the reaction</param>
        /// <param name="emote">Emote string of the reaction</param>
        /// <returns></returns>
        public Task LogReactionAddedEventAsync(ulong msgId, ulong userId, DateTimeOffset addedAt, string emote);
    }
}