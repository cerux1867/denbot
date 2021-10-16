using System;
using MongoDB.Bson;

namespace Denbot.Common.Entities {
    public abstract class Document : IDocument {
        public ObjectId Id { get; set; }
        public DateTimeOffset CreatedAt => Id.CreationTime;
    }
}