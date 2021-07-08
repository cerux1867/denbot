using System;

namespace Denbot.Ingest.Models {
    public record IngestStatus {
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? LastInteractionProcessedAt { get; set; }
        public int NumProcessedInteractions { get; set; }

        public IngestStatus() {
            NumProcessedInteractions = 0;
        }
    }
}