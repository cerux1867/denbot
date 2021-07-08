namespace Denbot.API.Models {
    public record MongoDbSettings {
        public string RemoveRoleVoteCollectionName { get; set; }
        public string GuildsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}