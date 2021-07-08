using System;
using System.ComponentModel.DataAnnotations;

namespace Denbot.Common.Models {
    public class CreatableRemoveRoleVote {
        [Required]
        public ulong TargetUserId { get; set; }
        
        [Required]
        public ulong InitiatingUserId { get; set; }
        
        [Required]
        public DateTimeOffset ExpiresAt { get; set; }
    }
}