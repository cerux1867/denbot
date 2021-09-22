namespace Denbot.Ingest.Models.Analytics {
    public record ReactionLog : AnalyticsLog {
        public override AnalyticsEventType EventType => AnalyticsEventType.Reaction;
        public string Emote { get; set; }
    }
}