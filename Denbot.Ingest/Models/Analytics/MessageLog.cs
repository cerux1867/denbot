namespace Denbot.Ingest.Models.Analytics {
    public record MessageLog : TextAnalyticsLogBase {
        public override AnalyticsEventType EventType => AnalyticsEventType.Message;
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public string Message { get; set; }
        public string[] Emotes { get; set; }
        public string[] Stickers { get; set; }
        public ulong[] UserMentions { get; set; }
        public ulong[] RoleMentions { get; set; }
        public ulong[] ChannelMentions { get; set; }
        public bool MentionsEveryone { get; set; }
        public string[] AttachmentMimeTypes { get; set; }
        public ulong? RepliedMessageId { get; set; }
        public ulong? ThreadId { get; set; }
    }
}