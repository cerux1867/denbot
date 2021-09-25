using System;
using System.Collections.Generic;

namespace Denbot.Ingest.Models {
    public class AnalyticsSettings {
        public bool Enabled { get; set; } = false;
        public Uri LogstashUrl { get; set; }
        public Dictionary<string, ulong[]> IgnoreDict { get; set; }
        public bool EventCorrelationEnabled { get; set; }
        public ElasticSearchSettings Elastic { get; set; }
        public string AnalyticsIndexName { get; set; }

        public AnalyticsSettings() {
            IgnoreDict = new Dictionary<string, ulong[]>();
        }
    }
}