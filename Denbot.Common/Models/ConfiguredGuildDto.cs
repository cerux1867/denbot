using System;

namespace Denbot.Common.Models {
    public class ConfiguredGuildDto {
        public string Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public ulong GuildId { get; set; }
        public ulong GuildOwnerId { get; set; }
        public GuildSettings Settings { get; set; }
    }
}