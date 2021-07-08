using System;

namespace Denbot.Common.Models {
    public record RemoveRoleBallot {
        public BallotType Type { get; set; }
        public ulong VoterId { get; set; }
        public DateTimeOffset CastAt { get; set; }
    }
}