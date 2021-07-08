using Denbot.Common.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Denbot.API.Entities {
    public record RemoveRoleVoteEntity : RemoveRoleVote {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("Id")]
        public override string VoteId { get; set; }
    }
}