using System.Collections.Generic;

namespace Denbot.Ingest.Results {
    public class FailureValueResult<T> : ValueResult<T> {
        public override ResultType Type { get; }
        public override List<string> Errors => new() {_error ?? "Operation failed"};
        public override T Value { get; }
        
        private readonly string _error;

        public FailureValueResult(string error, ResultType type) {
            _error = error;
            Type = type;
            Value = default;
        }
    }
}