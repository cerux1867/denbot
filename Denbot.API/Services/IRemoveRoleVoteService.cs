using System.Collections.Generic;
using System.Threading.Tasks;
using Denbot.Common.Entities;
using Denbot.Common.Models;

namespace Denbot.API.Services {
    public interface IRemoveRoleVoteService {
        public Task<List<RemoveRoleVoteEntity>> GetAllByGuildAsync(ulong guildId, VoteState? state);

        public Task<RemoveRoleVoteEntity> GetByIdAsync(string id);

        public Task<RemoveRoleVoteEntity> CreateInGuildAsync(ulong guildId, RemoveRoleVoteEntity vote);

        public Task<RemoveRoleVoteEntity> AddBallotAsync(string voteId, RemoveRoleBallot ballot);
    }
}