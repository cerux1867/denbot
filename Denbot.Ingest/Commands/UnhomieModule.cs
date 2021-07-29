using System;
using System.Linq;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Attributes;
using Denbot.Ingest.Jobs;
using Denbot.Ingest.Results;
using Denbot.Ingest.Services;
using DSharpPlusNextGen;
using DSharpPlusNextGen.Entities;
using DSharpPlusNextGen.Enums;
using DSharpPlusNextGen.SlashCommands;
using Quartz;

namespace Denbot.Ingest.Commands {
    public class UnhomieModule : SlashCommandModule {
        [SlashCommandGroup("unhomie", "Starts unhomie votes and configures the voting system")]
        public class Unhomie : SlashCommandModule {
            private readonly IRemoveRoleVoteService _removeRoleVoteService;
            private readonly ISchedulerFactory _schedulerFactory;

            public Unhomie(IRemoveRoleVoteService removeRoleVoteService, ISchedulerFactory schedulerFactory) {
                _removeRoleVoteService = removeRoleVoteService;
                _schedulerFactory = schedulerFactory;
            }

            [SlashIsOwnerOrRequireUserPermission(Permissions.Administrator)]
            [SlashCommand("configure", "Configures the role removal voting system")]
            public async Task ConfigureAsync(InteractionContext context,
                [Option("target_role", "Which roles can vote and be unroled")]
                DiscordRole role,
                [Option("enabled", "Determines if unhomieing is enabled or not")]
                bool isEnabled = true,
                [Option("quorum", "The minimum amount of total votes required")]
                long quorum = 10,
                [Option("period", "Number of minutes the user will be unroled for on a successful vote result")]
                long period = 10,
                [Option("timeout", "Number of minutes until a vote expires. Maximum of 10.")]
                long timeout = 10) {
                var interactionResponseBuilder = new DiscordInteractionResponseBuilder()
                    .AsEphemeral(true);
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    interactionResponseBuilder);
                var msgBuilder = new DiscordWebhookBuilder();

                var settings = await _removeRoleVoteService.CreateOrUpdateGuildRemoveRoleSettingsAsync(context.Guild.Id,
                    context.Guild.OwnerId, new RemoveRoleSettings {
                        Period = Convert.ToInt32(period),
                        Quorum = Convert.ToInt32(quorum),
                        Timeout = Convert.ToInt32(timeout),
                        IsEnabled = isEnabled,
                        TargetableRole = role.Id
                    });
                if (settings.Type != ResultType.Ok) {
                    await context.EditResponseAsync(
                        msgBuilder.WithContent("Error: An error occured while performing configuration changes"));
                    return;
                }

                await context.EditResponseAsync(msgBuilder.WithContent(
                    $"The following configuration changes have been applied:\n**Enabled:**: {settings.Value.IsEnabled}\n**Quorum**: {settings.Value.Quorum}\n**Period**: {settings.Value.Period}\n**Timeout**: {settings.Value.Timeout}\n**Targetable role**: {role.Mention}"));
            }

            [SlashCommand("vote",
                "Starts a unhomie vote on the targeted user.")]
            public async Task VoteAsync(InteractionContext context,
                [Option("target", "The user which you want to unhomie")]
                DiscordUser target) {
                var interactionResponseBuilder = new DiscordInteractionResponseBuilder();
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    interactionResponseBuilder);
                var msgBuilder = new DiscordWebhookBuilder();

                var unhomieSettings = await _removeRoleVoteService.GetGuildRemoveRoleSettingsAsync(context.Guild.Id);

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
                
                var voteResult =
                    await _removeRoleVoteService.StartVoteAsync(context.Guild.Id, context.User.Id, target.Id);
                if (voteResult.Type == ResultType.Conflict) {
                    await context.EditResponseAsync(msgBuilder.WithContent("Error: There is an on-going vote"));
                    return;
                }

                if (voteResult.Type != ResultType.Ok) {
                    await context.EditResponseAsync(msgBuilder.WithContent("Error: An unknown error has occured"));
                }

                var timeout = voteResult.Value.ExpiresAt;

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor(member.Nickname ?? $"{target.Username}#{target.Discriminator}", null, member.GuildAvatarUrl)
                    .WithColor(new DiscordColor("fff203"))
                    .WithTimestamp(DateTime.Now)
                    .WithTitle("Unhomie vote")
                    .WithDescription(
                        $"Vote to temporarily remove {target.Mention} from role **{targetableRole.Name}** for a period of **{unhomieSettings.Value.Period}** minutes. It will end once at least **{unhomieSettings.Value.Quorum}** members of role **{targetableRole.Name}** have cast their votes or it will time out in **{unhomieSettings.Value.Timeout}** minutes")
                    .WithFooter(context.User.Username, context.User.AvatarUrl);
                msgBuilder = new DiscordWebhookBuilder()
                    .AddEmbed(embed)
                    .AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"RoleRemovalBallot-{voteResult.Value.VoteId}-aye", "Aye"),
                        new DiscordButtonComponent(ButtonStyle.Danger, $"RoleRemovalBallot-{voteResult.Value.VoteId}-nay", "Nay"));
                await context.EditResponseAsync(msgBuilder);

                var job = JobBuilder.Create<VotePollJob>()
                    .WithIdentity(voteResult.Value.VoteId, "RoleRemovalVote")
                    .Build();
                job.JobDataMap.Put("interactionContext", context);
                job.JobDataMap.Put("originalEmbedBuilder", embed);
                job.JobDataMap.Put("targetableRole", targetableRole);
                job.JobDataMap.Put("voteId", voteResult.Value.VoteId);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"{voteResult.Value.VoteId}", "RoleRemovalVote")
                    .StartAt(DateTimeOffset.Now.AddSeconds(5))
                    .WithSimpleSchedule(x => {
                        x.WithIntervalInSeconds(5);
                        x.RepeatForever();
                    })
                    .EndAt(timeout)
                    .Build();
                var scheduler = await _schedulerFactory.GetScheduler();
                await scheduler.ScheduleJob(job, trigger);
            }

            [SlashIsOwner]
            [SlashCommand("override_restore", "Overrides the role restoration process")]
            public async Task OverrideRestoreAsync(InteractionContext ctx, [Option("target", "User to restore")] DiscordUser target) {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true));
                var memberTarget = await ctx.Guild.GetMemberAsync(target.Id);
                var guildSettings = await _removeRoleVoteService.GetGuildRemoveRoleSettingsAsync(ctx.Guild.Id);
                var targetRole = ctx.Guild.GetRole(guildSettings.Value.TargetableRole);
                await memberTarget.GrantRoleAsync(targetRole, "DenBot: Manual restore process override, should only be used if the bot fails");
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"User {target.Mention} has been restored to role {targetRole.Mention}"));
            }
        }
    }
}