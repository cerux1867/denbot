using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Models;
using Denbot.Ingest.Results;
using Microsoft.Extensions.Options;

namespace Denbot.Ingest.Services {
    public class RemoveRoleVoteHttpService : IRemoveRoleVoteService {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions;


        public RemoveRoleVoteHttpService(HttpClient client, IOptions<BackendSettings> backendSettings) {
            _client = client;
            _client.BaseAddress = backendSettings.Value.BaseUrl;
            _serializerOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<ValueResult<RemoveRoleVoteDto>> StartVoteAsync(ulong guildId, ulong initiatingUserId,
            ulong targetUserId) {
            var settingsResult = await GetGuildSettingsAsync(guildId);
            if (settingsResult.Type != ResultType.Ok) {
                return Result.NotFound<RemoveRoleVoteDto>("Guild is not configured");
            }

            var voteStartResult = await _client.PostAsync($"Guilds/{guildId}/Remove-Role-Votes",
                new StringContent(JsonSerializer.Serialize(new CreatableRemoveRoleVote {
                    TargetUserId = targetUserId,
                    InitiatingUserId = initiatingUserId,
                    ExpiresAt = DateTimeOffset.Now.AddMinutes(settingsResult.Value.Settings.RemoveRoleSettings.Timeout)
                }), Encoding.UTF8, "application/json"));
            if (voteStartResult.StatusCode == HttpStatusCode.Conflict) {
                return Result.Conflict<RemoveRoleVoteDto>("There is already an ongoing vote in this guild");
            }

            return Result.Ok(
                JsonSerializer.Deserialize<RemoveRoleVoteDto>(await voteStartResult.Content.ReadAsStringAsync(), _serializerOptions));
        }

        public async Task<ValueResult<RemoveRoleVoteDto>> GetVoteAsync(string voteId) {
            var voteResult = await _client.GetAsync($"Remove-Role-Votes/{voteId}");
            if (voteResult.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<RemoveRoleVoteDto>("Vote with the given ID was no found");
            }

            voteResult.EnsureSuccessStatusCode();
            return Result.Ok(JsonSerializer.Deserialize<RemoveRoleVoteDto>(await voteResult.Content.ReadAsStringAsync(), _serializerOptions));
        }

        public async Task<ValueResult<List<RemoveRoleVoteDto>>> GetAllGuildVotesAsync(ulong guildId) {
            var votesResult = await _client.GetAsync($"Guilds/{guildId}/Remove-Role-Votes");
            if (votesResult.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<List<RemoveRoleVoteDto>>("Guild with the given ID was not found");
            }
            votesResult.EnsureSuccessStatusCode();

            return Result.Ok(
                JsonSerializer.Deserialize<List<RemoveRoleVoteDto>>(await votesResult.Content.ReadAsStringAsync(),
                    _serializerOptions));
        }

        public async Task<ValueResult<RemoveRoleVoteDto>> CastBallotAsync(string voteId, BallotType type, ulong userId) {
            var voteResult = await _client.PostAsync($"Remove-Role-Votes/{voteId}/Ballots", new StringContent(
                JsonSerializer.Serialize(new RemoveRoleBallot {
                    Type = type,
                    CastAt = DateTimeOffset.Now,
                    VoterId = userId
                }), Encoding.UTF8, "application/json"));
            if (voteResult.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<RemoveRoleVoteDto>("Vote with the given ID was not found");
            }

            if (!voteResult.IsSuccessStatusCode) {
                return new FailureValueResult<RemoveRoleVoteDto>("An unknown error has occured", ResultType.Other);
            }

            return Result.Ok(JsonSerializer.Deserialize<RemoveRoleVoteDto>(await voteResult.Content.ReadAsStringAsync(), _serializerOptions));
        }

        public async Task<ValueResult<RemoveRoleSettings>> GetGuildRemoveRoleSettingsAsync(ulong guildId) {
            var settingsResult = await GetGuildSettingsAsync(guildId);
            if (settingsResult.Type == ResultType.NotFound) {
                return Result.NotFound<RemoveRoleSettings>("Guild is not configured");
            }

            return Result.Ok(settingsResult.Value.Settings.RemoveRoleSettings);
        }

        public async Task<ValueResult<RemoveRoleSettings>> CreateOrUpdateGuildRemoveRoleSettingsAsync(ulong guildId,
            ulong guildOwnerId, RemoveRoleSettings settings) {
            var settingsResult = await GetGuildSettingsAsync(guildId);
            CreatableGuild guild;
            if (settingsResult.Type == ResultType.NotFound) {
                // Create a guild
                guild = new CreatableGuild {
                    Id = guildId,
                    OwnerUserId = guildOwnerId,
                    RemoveRoleSettings = settings
                };
            }
            else {
                var settingsToSet = settingsResult.Value.Settings.RemoveRoleSettings;
                if (settingsToSet.IsEnabled != settings.IsEnabled) {
                    settingsToSet.IsEnabled = settings.IsEnabled;
                }

                if (settingsToSet.IsBackfireEnabled != settings.IsBackfireEnabled) {
                    settingsToSet.IsBackfireEnabled = settings.IsBackfireEnabled;
                }

                if (settingsToSet.Period != settings.Period) {
                    settingsToSet.Period = settings.Period;
                }

                if (settingsToSet.Quorum != settings.Quorum) {
                    settingsToSet.Quorum = settings.Quorum;
                }

                if (settingsToSet.Timeout != settings.Timeout) {
                    settingsToSet.Timeout = settings.Timeout;
                }

                if (settingsToSet.TargetableRole != settings.TargetableRole) {
                    settingsToSet.TargetableRole = settings.TargetableRole;
                }
                
                guild = new CreatableGuild {
                    Id = settingsResult.Value.GuildId,
                    OwnerUserId = settingsResult.Value.GuildOwnerId,
                    RemoveRoleSettings = settingsToSet
                };
            }

            var response = await _client.PostAsync("Guilds",
                new StringContent(JsonSerializer.Serialize(guild), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var guildResult = JsonSerializer.Deserialize<ConfiguredGuildDto>(await response.Content.ReadAsStringAsync(),
                _serializerOptions);
            return Result.Ok(guildResult.Settings.RemoveRoleSettings);
        }

        private async Task<ValueResult<ConfiguredGuildDto>> GetGuildSettingsAsync(ulong guildId) {
            var settingsResponse = await _client.GetAsync($"Guilds/{guildId}");
            if (settingsResponse.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<ConfiguredGuildDto>("This guild is not configured");
            }

            var settings =
                JsonSerializer.Deserialize<ConfiguredGuildDto>(await settingsResponse.Content.ReadAsStringAsync(), 
                    _serializerOptions);
            return Result.Ok(settings);
        }
    }
}