using System;
using System.Collections.Generic;
using Denbot.Common.Attributes;
using Denbot.Common.Models;

namespace Denbot.Common.Entities {
    [BsonCollection("removeRoleVotes")]
    public class RemoveRoleVoteEntity : Document {
        public ulong VoteId { get; set; }
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