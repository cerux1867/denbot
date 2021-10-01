namespace Denbot.Ingest.Models.Analytics {
    public abstract record TextAnalyticsLogBase : AnalyticsLog {
        public ulong MessageId { get; set; }
    }
}