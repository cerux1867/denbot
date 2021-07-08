namespace Denbot.Ingest.Models {
    public class UnhomieSettings {
        public bool IsEnabled { get; set; }
        public int Quorum { get; set; }
        public int Timeout { get; set; }
        public int Period { get; set; }
        public ulong TargetableRole { get; set; }
    }
}