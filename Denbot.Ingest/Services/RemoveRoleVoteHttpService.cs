using System;
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

        public async Task<ValueResult<RemoveRoleVote>> StartVoteAsync(ulong guildId, ulong initiatingUserId,
            ulong targetUserId) {
            var settingsResult = await GetGuildSettingsAsync(guildId);
            if (settingsResult.Type != ResultType.Ok) {
                return Result.NotFound<RemoveRoleVote>("Guild is not configured");
            }

            var voteStartResult = await _client.PostAsync($"Guilds/{guildId}/Remove-Role-Votes",
                new StringContent(JsonSerializer.Serialize(new CreatableRemoveRoleVote {
                    TargetUserId = targetUserId,
                    InitiatingUserId = initiatingUserId,
                    ExpiresAt = DateTimeOffset.Now.AddMinutes(settingsResult.Value.Settings.RemoveRoleSettings.Timeout)
                }), Encoding.UTF8, "application/json"));
            if (voteStartResult.StatusCode == HttpStatusCode.Conflict) {
                return Result.Conflict<RemoveRoleVote>("There is already an ongoing vote in this guild");
            }

            return Result.Ok(
                JsonSerializer.Deserialize<RemoveRoleVote>(await voteStartResult.Content.ReadAsStringAsync(), _serializerOptions));
        }

        public async Task<ValueResult<RemoveRoleVote>> GetVoteAsync(string voteId) {
            var voteResult = await _client.GetAsync($"Remove-Role-Votes/{voteId}");
            if (voteResult.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<RemoveRoleVote>("Vote with the given ID was no found");
            }

            voteResult.EnsureSuccessStatusCode();
            return Result.Ok(JsonSerializer.Deserialize<RemoveRoleVote>(await voteResult.Content.ReadAsStringAsync(), _serializerOptions));
        }

        public async Task<ValueResult<RemoveRoleVote>> CastBallotAsync(string voteId, BallotType type, ulong userId) {
            var voteResult = await _client.PostAsync($"Remove-Role-Votes/{voteId}/Ballots", new StringContent(
                JsonSerializer.Serialize(new RemoveRoleBallot {
                    Type = type,
                    CastAt = DateTimeOffset.Now,
                    VoterId = userId
                }), Encoding.UTF8, "application/json"));
            if (voteResult.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<RemoveRoleVote>("Vote with the given ID was not found");
            }

            if (!voteResult.IsSuccessStatusCode) {
                return new FailureValueResult<RemoveRoleVote>("An unknown error has occured", ResultType.Other);
            }

            return Result.Ok(JsonSerializer.Deserialize<RemoveRoleVote>(await voteResult.Content.ReadAsStringAsync(), _serializerOptions));
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
                guild = new CreatableGuild {
                    Id = settingsResult.Value.GuildId,
                    OwnerUserId = settingsResult.Value.GuildOwnerId,
                    RemoveRoleSettings = settings
                };
            }

            var response = await _client.PostAsync("Guilds",
                new StringContent(JsonSerializer.Serialize(guild), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var guildResult = JsonSerializer.Deserialize<ConfiguredGuild>(await response.Content.ReadAsStringAsync(), _serializerOptions);
            return Result.Ok(guildResult.Settings.RemoveRoleSettings);
        }

        private async Task<ValueResult<ConfiguredGuild>> GetGuildSettingsAsync(ulong guildId) {
            var settingsResponse = await _client.GetAsync($"Guilds/{guildId}");
            if (settingsResponse.StatusCode == HttpStatusCode.NotFound) {
                return Result.NotFound<ConfiguredGuild>("This guild is not configured");
            }

            var settings =
                JsonSerializer.Deserialize<ConfiguredGuild>(await settingsResponse.Content.ReadAsStringAsync(), _serializerOptions);
            return Result.Ok(settings);
        }
    }
}