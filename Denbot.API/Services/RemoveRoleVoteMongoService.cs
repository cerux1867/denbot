using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Denbot.API.Entities;
using Denbot.API.Models;
using Denbot.Common.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Denbot.API.Services {
    public class RemoveRoleVoteMongoService : IRemoveRoleVoteService {
        private readonly IGuildsService _guildsService;
        private readonly IMongoCollection<RemoveRoleVoteEntity> _votes;

        public RemoveRoleVoteMongoService(IMongoDatabase db, IGuildsService guildsService, IOptions<MongoDbSettings> settings) {
            _guildsService = guildsService;
            _votes = db.GetCollection<RemoveRoleVoteEntity>(settings.Value.RemoveRoleVoteCollectionName);
        }

        public async Task<List<RemoveRoleVoteEntity>> GetAllByGuildAsync(ulong guildId, VoteState? state) {
            await RefreshOngoingVoteStateAsync();
            return state.HasValue
                ? await _votes.AsQueryable().Where(v => v.State == state.Value && v.GuildId == guildId).ToListAsync()
                : await _votes.AsQueryable().Where(v => v.GuildId == guildId).ToListAsync();
        }

        public async Task<RemoveRoleVoteEntity> GetByIdAsync(string id) {
            await RefreshOngoingVoteStateAsync();
            return await _votes.AsQueryable().FirstOrDefaultAsync(v => v.VoteId == id);
        }

        public async Task<RemoveRoleVoteEntity> CreateInGuildAsync(ulong guildId, RemoveRoleVoteEntity vote) {
            await RefreshOngoingVoteStateAsync();
            var ongoingVote = await _votes.AsQueryable().FirstOrDefaultAsync(v => v.State == VoteState.Ongoing && v.GuildId == guildId);
            if (ongoingVote != null) {
                return null;
            }
            
            await _votes.InsertOneAsync(vote);

            return vote;
        }

        public async Task<RemoveRoleVoteEntity> AddBallotAsync(string voteId, RemoveRoleBallot ballot) {
            await RefreshOngoingVoteStateAsync();
            var ongoingVote = await _votes.AsQueryable()
                .FirstOrDefaultAsync(v => v.State == VoteState.Ongoing && v.VoteId == voteId);
            if (ongoingVote == null) {
                return null;
            }

            var existingBallot = ongoingVote.Ballots.FirstOrDefault(b => b.VoterId == ballot.VoterId);
            if (existingBallot != null) {
                var filterBuilder = Builders<RemoveRoleVoteEntity>.Filter;
                var filter = filterBuilder
                    .Eq(x => x.VoteId, voteId) & filterBuilder
                    .ElemMatch(doc => doc.Ballots, el => el.VoterId == ballot.VoterId);
                var updateBuilder = Builders<RemoveRoleVoteEntity>.Update;
                var update = updateBuilder.Set(doc => doc.Ballots[-1].Type, ballot.Type)
                    .Set(doc => doc.LastUpdatedAt, DateTimeOffset.Now)
                    .Set(doc => doc.Ballots[-1].CastAt, ballot.CastAt);

                await _votes.FindOneAndUpdateAsync(filter, update);
            }
            else {
                var filter = Builders<RemoveRoleVoteEntity>.Filter
                    .Where(x => x.VoteId == voteId);
                var update = Builders<RemoveRoleVoteEntity>.Update
                    .Set(doc => doc.LastUpdatedAt, DateTimeOffset.Now)
                    .Push(doc => doc.Ballots, ballot);
                await _votes.FindOneAndUpdateAsync(filter, update);
            }
            await RefreshOngoingVoteStateAsync();
            return await _votes.AsQueryable().FirstOrDefaultAsync(v => v.VoteId == voteId);
        }

        private async Task RefreshOngoingVoteStateAsync() {
            var votes = await _votes.AsQueryable()
                .Where(v => v.State == VoteState.Ongoing).ToListAsync();
            foreach (var vote in votes) {
                var guild = await _guildsService.GetByIdAsync(vote.GuildId);
                if (DateTimeOffset.Now >= vote.ExpiresAt || vote.Ballots.Count >= guild.Settings.RemoveRoleSettings.Quorum) {
                    var ayeCount = vote.Ballots.Count(b => b.Type == BallotType.Aye);
                    var nayCount = vote.Ballots.Count(b => b.Type == BallotType.Nay);
                    var filter = Builders<RemoveRoleVoteEntity>.Filter
                        .Where(x => x.VoteId == vote.VoteId);
                    var stateToUpdateTo = VoteState.Expired;
                    if (ayeCount > nayCount) {
                        stateToUpdateTo = VoteState.Passed;
                    }
                    else if (ayeCount < nayCount) {
                        stateToUpdateTo = VoteState.Failed;
                    }

                    var update = Builders<RemoveRoleVoteEntity>.Update
                        .Set(doc => doc.LastUpdatedAt, DateTimeOffset.Now)
                        .Set(doc => doc.State, stateToUpdateTo);
                    await _votes.FindOneAndUpdateAsync(filter, update);
                }
            }
        }
    }
}