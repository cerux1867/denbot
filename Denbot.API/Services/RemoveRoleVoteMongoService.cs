using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Denbot.Common.Entities;
using Denbot.Common.Models;
using Denbot.Common.Repositories;
using MongoDB.Bson;

namespace Denbot.API.Services {
    public class RemoveRoleVoteMongoService : IRemoveRoleVoteService {
        private readonly IGuildsService _guildsService;
        private readonly IMongoRepository<RemoveRoleVoteEntity> _votes;

        public RemoveRoleVoteMongoService(IMongoRepository<RemoveRoleVoteEntity> db, IGuildsService guildsService) {
            _guildsService = guildsService;
            _votes = db;
        }

        public async Task<List<RemoveRoleVoteEntity>> GetAllByGuildAsync(ulong guildId, VoteState? state) {
            await RefreshOngoingVoteStateAsync();
            if (state.HasValue) {
                return _votes
                    .FilterBy(v => v.State == state.Value && v.GuildId == guildId).ToList();
            }

            return _votes.FilterBy(v => v.GuildId == guildId).ToList();
        }

        public async Task<RemoveRoleVoteEntity> GetByIdAsync(string id) {
            await RefreshOngoingVoteStateAsync();
            return await _votes.FindByIdAsync(id);
        }

        public async Task<RemoveRoleVoteEntity> CreateInGuildAsync(ulong guildId, RemoveRoleVoteEntity vote) {
            await RefreshOngoingVoteStateAsync();
            var ongoingVote = await _votes
                .FindOneAsync(v => v.State == VoteState.Ongoing && v.GuildId == guildId);
            if (ongoingVote != null) {
                return null;
            }

            await _votes.InsertOneAsync(vote);

            return vote;
        }

        public async Task<RemoveRoleVoteEntity> AddBallotAsync(string voteId, RemoveRoleBallot ballot) {
            await RefreshOngoingVoteStateAsync();
            var ongoingVote = await _votes
                .FindOneAsync(v => v.State == VoteState.Ongoing && v.Id == ObjectId.Parse(voteId));
            if (ongoingVote == null) {
                return null;
            }

            var existingBallot = ongoingVote.Ballots.FirstOrDefault(b => b.VoterId == ballot.VoterId);
            if (existingBallot != null) {
                existingBallot.Type = ballot.Type;
                existingBallot.CastAt = DateTimeOffset.Now;
                await _votes.ReplaceOneAsync(ongoingVote);
            }
            else {
                ongoingVote.Ballots.Add(new RemoveRoleBallot {
                    Type = ballot.Type,
                    CastAt = DateTimeOffset.Now,
                    VoterId = ballot.VoterId
                });
                await _votes.ReplaceOneAsync(ongoingVote);
            }

            await RefreshOngoingVoteStateAsync();
            return await _votes.FindOneAsync(v => v.Id == ObjectId.Parse(voteId));
        }

        private async Task RefreshOngoingVoteStateAsync() {
            var votes = _votes.AsQueryable();
            foreach (var vote in votes) {
                var guild = await _guildsService.GetByIdAsync(vote.GuildId);
                if (DateTimeOffset.Now >= vote.ExpiresAt ||
                    vote.Ballots.Count >= guild.Settings.RemoveRoleSettings.Quorum) {
                    await FinaliseVoteAsync(vote);
                }
                else if (vote.Ballots.FirstOrDefault(b => b.VoterId == vote.TargetUserId) != null) {
                    await FinaliseVoteAsync(vote);
                }
            }
        }

        private async Task FinaliseVoteAsync(RemoveRoleVoteEntity vote) {
            var ayeCount = vote.Ballots.Count(b => b.Type == BallotType.Aye);
            var nayCount = vote.Ballots.Count(b => b.Type == BallotType.Nay);
            var stateToUpdateTo = VoteState.Expired;
            if (vote.Ballots.FirstOrDefault(b => b.VoterId == vote.TargetUserId) != null) {
                stateToUpdateTo = VoteState.SelfX;
            }
            else if (ayeCount > nayCount) {
                stateToUpdateTo = VoteState.Passed;
            }
            else if (ayeCount < nayCount) {
                stateToUpdateTo = VoteState.Failed;
            }

            vote.State = stateToUpdateTo;
            vote.LastUpdatedAt = DateTimeOffset.Now;
            await _votes.ReplaceOneAsync(vote);
        }
    }
}