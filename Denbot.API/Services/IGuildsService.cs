using System.Threading.Tasks;
using Denbot.Common.Entities;

namespace Denbot.API.Services {
    public interface IGuildsService {
        public Task<ConfiguredGuildEntity> GetByIdAsync(ulong guildId);

        public Task<ConfiguredGuildEntity> CreateAsync(ConfiguredGuildEntity guildEntity);
    }
}