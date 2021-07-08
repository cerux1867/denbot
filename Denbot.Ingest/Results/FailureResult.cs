using System.Collections.Generic;

namespace Denbot.Ingest.Results {
    public class FailureResult : Result{
        public override ResultType Type { get; }
        public override List<string> Errors => new() {_error ?? "Operation failed"};

        private readonly string _error;

        public FailureResult(string error, ResultType type) {
            _error = error;
            Type = type;
        }
    }
}