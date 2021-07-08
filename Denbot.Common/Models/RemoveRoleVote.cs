using System;
using System.Collections.Generic;

namespace Denbot.Common.Models {
    public record RemoveRoleVote {
        public virtual string VoteId { get; set; }
        public ulong GuildId { get; set; }
        public ulong TargetUserId { get; set; }
        public ulong InitiatingUserId { get; set; }
        public List<RemoveRoleBallot> Ballots { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset LastUpdatedAt { get; set; }
        public VoteState State { get; set; }
    }
}