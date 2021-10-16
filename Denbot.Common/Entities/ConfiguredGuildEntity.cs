using Denbot.Common.Attributes;
using Denbot.Common.Models;

namespace Denbot.Common.Entities {
    [BsonCollection("guilds")]
    public class ConfiguredGuildEntity : Document {
        public ulong GuildId { get; set; }
        public ulong GuildOwnerId { get; set; }
        public GuildSettings Settings { get; set; }
    }
}