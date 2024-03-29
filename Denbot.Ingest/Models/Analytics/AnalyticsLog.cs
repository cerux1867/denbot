﻿using System;

namespace Denbot.Ingest.Models.Analytics {
    public abstract record AnalyticsLog {
        public ulong UserId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public abstract AnalyticsEventType EventType { get; }
    }
}