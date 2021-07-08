using System.Threading.Tasks;
using Denbot.API.Entities;
using Denbot.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Denbot.API.Services {
    public class GuildsMongoService : IGuildsService {
        private readonly IMongoCollection<ConfiguredGuildEntity> _guildsCollection;

        public GuildsMongoService(IMongoDatabase db, IOptions<MongoDbSettings> settings) {
            _guildsCollection =
                db.GetCollection<ConfiguredGuildEntity>(settings.Value.GuildsCollectionName);
        }

        public async Task<ConfiguredGuildEntity> GetByIdAsync(ulong guildId) {
            var existingDoc = await _guildsCollection.AsQueryable()
                .FirstOrDefaultAsync(doc => doc.GuildId == guildId);
            return existingDoc;
        }

        public async Task<ConfiguredGuildEntity> CreateAsync(ConfiguredGuildEntity guildEntity) {
            var filter = Builders<ConfiguredGuildEntity>.Filter
                .Eq(doc => doc.GuildId, guildEntity.GuildId);
            var result=  await _guildsCollection.FindOneAndReplaceAsync(filter, guildEntity, new FindOneAndReplaceOptions<ConfiguredGuildEntity> {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            });
            return result;
        }
    }
}