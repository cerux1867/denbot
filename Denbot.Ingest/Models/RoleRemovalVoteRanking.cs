using System;

namespace Denbot.Ingest.Models {
    public record RoleRemovalVoteRanking {
        public ulong UserId { get; set; }
        public int PassedCount { get; set; }
        public int TotalCount { get; set; }
        public TimeSpan Fastest { get; set; }
    }
}