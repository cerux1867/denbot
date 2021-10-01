using System;

namespace Denbot.Ingest.Models.Analytics {
    public record VoiceActivityLog : AnalyticsLog {
        public override AnalyticsEventType EventType { get; }
        public ulong VoiceChannelId { get; set; }
        public ulong GuildId { get; set; }

        public VoiceActivityLog(AnalyticsEventType voiceEventType, ulong voiceChannelId) {
            if (voiceEventType is AnalyticsEventType.Message or AnalyticsEventType.Reaction) {
                throw new ArgumentOutOfRangeException(nameof(voiceEventType), voiceEventType,
                    "VoiceActivityLog can only record VoiceActivity events");
            }
            EventType = voiceEventType;
            VoiceChannelId = voiceChannelId;
        }
    }
}