using System.Collections.Generic;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Results;

namespace Denbot.Ingest.Services {
    public interface IRemoveRoleVoteService {
        public Task<ValueResult<RemoveRoleVote>> StartVoteAsync(ulong guildId, ulong initiatingUserId, ulong targetUserId);
        public Task<ValueResult<RemoveRoleVote>> GetVoteAsync(string voteId);
        public Task<ValueResult<List<RemoveRoleVote>>> GetAllGuildVotesAsync(ulong guildId);
        public Task<ValueResult<RemoveRoleVote>> CastBallotAsync(string voteId, BallotType type, ulong userId);

        public Task<ValueResult<RemoveRoleSettings>> GetGuildRemoveRoleSettingsAsync(ulong guildId);
        public Task<ValueResult<RemoveRoleSettings>> CreateOrUpdateGuildRemoveRoleSettingsAsync(ulong guildId, ulong guildOwnerId, RemoveRoleSettings settings);
    }
}