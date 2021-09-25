using System.Threading.Tasks;
using Denbot.Ingest.Models.Analytics;

namespace Denbot.Ingest.Services {
    public interface IAnalyticsCorrelationService {
        public Task DeleteMessageIfExistsAsync(ulong messageId);

        public Task UpdateMessageIfExistsAsync(MessageLog msg);

        public Task DeleteReactionIfExistsAsync(ulong msgId, ulong userId);
    }
}