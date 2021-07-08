using System.Threading.Tasks;
using Denbot.API.Entities;

namespace Denbot.API.Services {
    public interface IGuildsService {
        public Task<ConfiguredGuildEntity> GetByIdAsync(ulong guildId);

        public Task<ConfiguredGuildEntity> CreateAsync(ConfiguredGuildEntity guildEntity);
    }
}