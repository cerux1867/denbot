namespace Denbot.Common.Models {
    public class ConfiguredGuild {
        public ulong GuildId { get; set; }
        public ulong GuildOwnerId { get; set; }
        
        public GuildSettings Settings { get; set; }
    }
}