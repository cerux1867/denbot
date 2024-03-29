﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Services;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Denbot.Ingest.Jobs {
    public class VotePollJob : IJob {
        private readonly IRemoveRoleVoteService _removeRoleVoteService;
        private readonly ILogger<VotePollJob> _logger;

        public VotePollJob(IRemoveRoleVoteService removeRoleVoteService, ILogger<VotePollJob> logger) {
            _removeRoleVoteService = removeRoleVoteService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context) {
            var interactionContextInstance = (BaseContext) context.MergedJobDataMap["interactionContext"];
            var orgEmbedBuilder = (DiscordEmbedBuilder) context.MergedJobDataMap["originalEmbedBuilder"];
            var voteId = (string) context.MergedJobDataMap["voteId"];

            var vote = await _removeRoleVoteService.GetVoteAsync(voteId);
            var ayeCount = vote.Value.Ballots.Count(x => x.Type == BallotType.Aye);
            var nayCount = vote.Value.Ballots.Count(x => x.Type == BallotType.Nay);
            
            if (vote.Value.State != VoteState.Ongoing) {
                var target = await interactionContextInstance.Guild.GetMemberAsync(vote.Value.TargetUserId);
                var guildSettings =
                    await _removeRoleVoteService.GetGuildRemoveRoleSettingsAsync(interactionContextInstance.Guild.Id);
                var targetableRole = interactionContextInstance.Guild.GetRole(guildSettings.Value.TargetableRole);
                if (vote.Value.State is VoteState.Passed or VoteState.SelfX) {
                    await RevokeRoleAndScheduleRestoreAsync(target, targetableRole, vote.Value,
                        interactionContextInstance, guildSettings.Value, context);
                } else if (vote.Value.State == VoteState.Failed) {
                    if (guildSettings.Value.IsBackfireEnabled) {
                        var voteInitiator = await interactionContextInstance.Guild
                            .GetMemberAsync(vote.Value.InitiatingUserId);
                        await RevokeRoleAndScheduleRestoreAsync(voteInitiator, targetableRole, vote.Value,
                            interactionContextInstance, guildSettings.Value, context);
                    }
                }

                var stateString = vote.Value.State switch {
                    VoteState.Expired => "Expired",
                    VoteState.Failed => "Failed",
                    VoteState.Passed => "Passed",
                    VoteState.SelfX => "Cucked"
                };
                var voteResultString = $"**{stateString}** <t:{vote.Value.LastUpdatedAt.ToUnixTimeSeconds()}:R> with the final tally:\n";
                orgEmbedBuilder
                    .ClearFields()
                    .WithDescription(orgEmbedBuilder.Description + $"\n\n{voteResultString}")
                    .AddField("Aye", ayeCount.ToString(), true)
                    .AddField("Nay", nayCount.ToString(), true);
                var followupEmbed = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor("fff203"))
                    .WithTitle("Unhomie vote result")
                    .WithAuthor(target.Nickname ?? $"{target.Username}#{target.Discriminator}", null, target.GuildAvatarUrl)
                    .WithTimestamp(vote.Value.LastUpdatedAt)
                    .WithDescription(voteResultString)
                    .WithFooter(interactionContextInstance.Guild.Name, interactionContextInstance.Guild.IconUrl)
                    .AddField("Aye", ayeCount.ToString(), true)
                    .AddField("Nay", nayCount.ToString(), true);
                await interactionContextInstance.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(orgEmbedBuilder)
                    .AddComponents(
                        new DiscordButtonComponent(ButtonStyle.Success, $"RoleRemovalBallot-{voteId}-aye", "Aye", true),
                        new DiscordButtonComponent(ButtonStyle.Danger, $"RoleRemovalBallot-{voteId}-nay", "Nay",
                            true)));
                await interactionContextInstance.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .AddEmbed(followupEmbed));
                await context.Scheduler.DeleteJob(context.JobDetail.Key);
            }
            else {
                orgEmbedBuilder
                    .ClearFields()
                    .AddField("Aye", ayeCount.ToString(), true)
                    .AddField("Nay", nayCount.ToString(), true);
                await interactionContextInstance.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(orgEmbedBuilder)
                    .AddComponents(
                        new DiscordButtonComponent(ButtonStyle.Success, $"RoleRemovalBallot-{voteId}-aye", "Aye"),
                        new DiscordButtonComponent(ButtonStyle.Danger, $"RoleRemovalBallot-{voteId}-nay", "Nay")));
            }
        }

        private async Task RevokeRoleAndScheduleRestoreAsync(DiscordMember target, DiscordRole targetableRole, 
            RemoveRoleVoteDto vote, BaseContext discordContext, RemoveRoleSettings guildSettings, 
            IJobExecutionContext jobContext) {
            await target.RevokeRoleAsync(targetableRole);
            var restoreJob = JobBuilder.Create<RoleRestoreJob>()
                .WithIdentity($"{vote.Id}-Restore", "RoleRemovalVote")
                .Build();
            restoreJob.JobDataMap.Put("interactionContext", discordContext);
            restoreJob.JobDataMap.Put("targetRole", targetableRole);
            restoreJob.JobDataMap.Put("targetUser", target);
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{vote.Id}-restore", "RoleRemovalVote")
                .StartAt(DateTimeOffset.Now.AddMinutes(guildSettings.Period))
                .Build();
            await jobContext.Scheduler.ScheduleJob(restoreJob, trigger);
        } 
    }
}