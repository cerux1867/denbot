using System.Text.Json.Serialization;

namespace Denbot.Common.Models {
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VoteState {
        Ongoing,
        Passed,
        Failed,
        Expired
    }
}