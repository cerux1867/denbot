using System.Threading.Tasks;
using Denbot.Ingest.Models.Analytics;

namespace Denbot.Ingest.Services {
    public interface IAnalyticsCorrelationService {
        /// <summary>
        /// Deletes analytical events related to message with <paramref name="messageId"/>. Also deletes reaction events
        /// related to this message.
        /// </summary>
        /// <param name="messageId">Discord message ID</param>
        public Task DeleteMessageIfExistsAsync(ulong messageId);
        /// <summary>
        /// Updates the content of the analytical event related to the <paramref name="msg"/>
        /// </summary>
        /// <param name="msg">Analytical message corresponding to a Discord message</param>
        public Task UpdateMessageIfExistsAsync(MessageLog msg);
        /// <summary>
        /// Deletes the reaction analytical event related to the message with <paramref name="msgId"/> by user <paramref name="userId"/> 
        /// </summary>
        /// <param name="msgId">Discord message ID</param>
        /// <param name="userId">Discord user ID</param>
        public Task DeleteReactionIfExistsAsync(ulong msgId, ulong userId);
    }
}