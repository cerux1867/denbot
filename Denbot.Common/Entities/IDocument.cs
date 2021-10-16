using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Denbot.Common.Entities {
    public interface IDocument {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        ObjectId Id { get; set; }
        DateTimeOffset CreatedAt { get; }
    }
}