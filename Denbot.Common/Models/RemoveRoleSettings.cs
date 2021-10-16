namespace Denbot.Common.Models {
    public class RemoveRoleSettings {
        public bool IsEnabled { get; set; }
        public bool IsBackfireEnabled { get; set; } = true;
        public int Quorum { get; set; }
        public int Timeout { get; set; }
        public int Period { get; set; }
        public ulong TargetableRole { get; set; }
    }
}