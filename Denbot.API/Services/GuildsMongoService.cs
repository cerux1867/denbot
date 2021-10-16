using System.Threading.Tasks;
using Denbot.Common.Entities;
using Denbot.Common.Repositories;

namespace Denbot.API.Services {
    public class GuildsMongoService : IGuildsService {
        private readonly IMongoRepository<ConfiguredGuildEntity> _configuredGuildEntityRepo;

        public GuildsMongoService(IMongoRepository<ConfiguredGuildEntity> configuredGuildRepo) {
            _configuredGuildEntityRepo = configuredGuildRepo;
        }

        public async Task<ConfiguredGuildEntity> GetByIdAsync(ulong guildId) {
            var existingDoc = await _configuredGuildEntityRepo
                .FindOneAsync(entity => entity.GuildId == guildId);
            return existingDoc;
        }

        public async Task<ConfiguredGuildEntity> CreateAsync(ConfiguredGuildEntity guildEntity) {
            var existingGuild = await _configuredGuildEntityRepo
                .FindOneAsync(v => guildEntity.GuildId == v.GuildId);
            if (existingGuild != null) {
                guildEntity.Id = existingGuild.Id;
                await _configuredGuildEntityRepo.ReplaceOneAsync(guildEntity);
                
            }
            else {
                await _configuredGuildEntityRepo.InsertOneAsync(guildEntity);
            }
            var result = await _configuredGuildEntityRepo
                .FindOneAsync(v => v.GuildId == guildEntity.GuildId);
            return result;
        }
    }
}