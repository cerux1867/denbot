using System;
using System.Collections.Generic;

namespace Denbot.Ingest.Results {
    public abstract class Result {
        public abstract ResultType Type { get; }
        public abstract List<string> Errors { get; }

        public static SuccessValueResult<T> Ok<T>(T value) {
            return new(value);
        }

        public static SuccessResult Ok() {
            return new();
        }

        public static FailureResult NotFound(string error) {
            return new(error, ResultType.NotFound);
        }
        
        public static FailureValueResult<T> Conflict<T>(string error) {
            return new(error, ResultType.NotFound);
        }

        public static FailureValueResult<T> NotFound<T>(string error) {
            return new(error, ResultType.NotFound);
        }

        public bool IsSuccess() {
            return Type == ResultType.Ok;
        }

        public void EnsureSuccess() {
            if (Type != ResultType.Ok) {
                throw new Exception($"The operation did not succeed. The errors were: {string.Join(",", Errors)}");
            }
        }
    }
}