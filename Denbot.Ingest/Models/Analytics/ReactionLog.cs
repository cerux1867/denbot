namespace Denbot.Ingest.Models.Analytics {
    public record ReactionLog : TextAnalyticsLogBase {
        public override AnalyticsEventType EventType => AnalyticsEventType.Reaction;
        public string Emote { get; set; }
    }
}