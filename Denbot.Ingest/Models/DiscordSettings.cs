namespace Denbot.Ingest.Models {
    public record DiscordSettings {
        public string Token { get; set; }
        public ulong[] SlashCommandServers { get; set; }
    }
}