using System.ComponentModel.DataAnnotations;

namespace Denbot.Common.Models {
    public class CreatableGuild {
        [Required]
        public ulong Id { get; set; }
        [Required]
        public ulong OwnerUserId { get; set; }
        [Required]
        public RemoveRoleSettings RemoveRoleSettings { get; set; }
    }
}