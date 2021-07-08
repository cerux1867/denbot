using System.Collections.Generic;

namespace Denbot.Ingest.Results {
    public class SuccessResult : Result {
        public override ResultType Type => ResultType.Ok;
        public override List<string> Errors => new();
    }
}