using System;
using System.Collections.Generic;

namespace Denbot.Ingest.Models {
    public class AnalyticsSettings {
        public Uri LogstashUrl { get; set; }
        public Dictionary<string, ulong[]> IgnoreDict { get; set; }

        public AnalyticsSettings() {
            IgnoreDict = new Dictionary<string, ulong[]>();
        }
    }
}