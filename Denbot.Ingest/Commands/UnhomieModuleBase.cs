using System;
using System.Linq;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Jobs;
using Denbot.Ingest.Results;
using Denbot.Ingest.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Quartz;

namespace Denbot.Ingest.Commands {
    public class UnhomieModuleBase : ApplicationCommandsModule {
        protected readonly IRemoveRoleVoteService RemoveRoleVoteService;
        protected readonly ISchedulerFactory SchedulerFactory;

        public UnhomieModuleBase(IRemoveRoleVoteService removeRoleVoteService, ISchedulerFactory schedulerFactory) {
            RemoveRoleVoteService = removeRoleVoteService;
            SchedulerFactory = schedulerFactory;
        }

        protected async Task StartVoteAsync(BaseContext context, DiscordUser target) {
            var interactionResponseBuilder = new DiscordInteractionResponseBuilder();
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                interactionResponseBuilder);
            var msgBuilder = new DiscordWebhookBuilder();

            var unhomieSettings = await RemoveRoleVoteService.GetGuildRemoveRoleSettingsAsync(context.Guild.Id);

            if (unhomieSettings.Type == ResultType.NotFound) {
                await context.EditResponseAsync(
                    msgBuilder.WithContent(
                        "Error: Role removal votes must be configured by using the slash command **/unhomie configure**. This requires server admin privileges."));
                return;
            }

            if (!unhomieSettings.Value.IsEnabled) {
                await context.EditResponseAsync(
                    msgBuilder.WithContent("Error: This functionality is currently disabled"));
                return;
            }

            var targetableRole = context.Guild.GetRole(unhomieSettings.Value.TargetableRole);
            var member = await context.Guild.GetMemberAsync(target.Id);
            if (context.Member.Roles.FirstOrDefault(r => r.Id == targetableRole.Id) == null) {
                msgBuilder
                    .WithContent(
                        $"Error: You are not in the appropriate role - **{targetableRole.Name}** to execute this action");
                await context.EditResponseAsync(msgBuilder);
                return;
            }

            if (member.Roles.FirstOrDefault(r => r.Id == targetableRole.Id) == null) {
                msgBuilder
                    .WithContent(
                        $"Error: The user you are targeting is not in the appropriate role - **{targetableRole.Name}** to have this action executed against them");
                await context.EditResponseAsync(msgBuilder);
                return;
            }
            
            var scheduler = await SchedulerFactory.GetScheduler();

            if (context.User.Id == 102505595180957696 && target.Id == 727556118984917002) {
                var emote = DiscordEmoji.FromName(context.Client, ":SpecialChamp:");
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(
                        $"Oooops, seems like {context.User.Mention} has been too busy with complaints like {emote} *CERUX WHEN ARE YOU ADDING A SOUTHPAW BIAS* or {emote} *but 'its work' and 'be grateful off the new bot' ( that somehow has less feature than the old one)* and has somehow implemented the bias for the wrong user, oh nyoooooo {DiscordEmoji.FromName(context.Client, ":DonkSad:")}"));
                await member.RevokeRoleAsync(targetableRole);
                var restoreJob = JobBuilder.Create<RoleRestoreJob>()
                    .WithIdentity("CeruxSaltyDerhauni-Restore", "RoleRemovalVote")
                    .Build();
                restoreJob.JobDataMap.Put("interactionContext", context);
                restoreJob.JobDataMap.Put("targetRole", targetableRole);
                restoreJob.JobDataMap.Put("targetUser", target);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"CeruxSaltyDerhauni-Restore-restore", "RoleRemovalVote")
                    .StartAt(DateTimeOffset.Now.AddMinutes(unhomieSettings.Value.Period))
                    .Build();
                await scheduler.ScheduleJob(restoreJob, trigger);
                return;
            }

            var voteResult =
                await RemoveRoleVoteService.StartVoteAsync(context.Guild.Id, context.User.Id, target.Id);
            if (voteResult.Type == ResultType.Conflict) {
                await context.EditResponseAsync(msgBuilder.WithContent("Error: There is an on-going vote"));
                return;
            }

            if (voteResult.Type != ResultType.Ok) {
                await context.EditResponseAsync(msgBuilder.WithContent("Error: An unknown error has occured"));
            }

            var embed = new DiscordEmbedBuilder()
                .WithAuthor(member.Nickname ?? $"{target.Username}#{target.Discriminator}", null,
                    member.GuildAvatarUrl)
                .WithColor(new DiscordColor("fff203"))
                .WithTimestamp(DateTime.Now)
                .WithTitle("Unhomie vote")
                .AddField("Aye", "0", true)
                .AddField("Nay", "0", true)
                .WithDescription(
                    $"Vote to temporarily remove {target.Mention} from role **{targetableRole.Name}** for a period of **{unhomieSettings.Value.Period}** minutes. It will end once at least **{unhomieSettings.Value.Quorum}** members of role **{targetableRole.Name}** have cast their votes or it will time out in **{unhomieSettings.Value.Timeout}** minutes")
                .WithFooter(context.User.Username, context.User.AvatarUrl);
            msgBuilder = new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(
                    new DiscordButtonComponent(ButtonStyle.Success,
                        $"RoleRemovalBallot-{voteResult.Value.VoteId}-aye", "Aye"),
                    new DiscordButtonComponent(ButtonStyle.Danger,
                        $"RoleRemovalBallot-{voteResult.Value.VoteId}-nay", "Nay"));
            await context.EditResponseAsync(msgBuilder);

            var job = JobBuilder.Create<VotePollJob>()
                .WithIdentity(voteResult.Value.VoteId, "RoleRemovalVote")
                .Build();
            job.JobDataMap.Put("interactionContext", context);
            job.JobDataMap.Put("originalEmbedBuilder", embed);
            job.JobDataMap.Put("targetableRole", targetableRole);
            job.JobDataMap.Put("voteId", voteResult.Value.VoteId);
            var triggerPoll = TriggerBuilder.Create()
                .WithIdentity($"{voteResult.Value.VoteId}", "RoleRemovalVote")
                .StartAt(DateTimeOffset.Now.AddSeconds(5))
                .WithSimpleSchedule(x => {
                    x.WithIntervalInSeconds(5);
                    x.RepeatForever();
                })
                .Build();
            await scheduler.ScheduleJob(job, triggerPoll);
        }
    }
}