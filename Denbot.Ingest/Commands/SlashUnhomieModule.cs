using System;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Attributes;
using Denbot.Ingest.Results;
using Denbot.Ingest.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using Quartz;

namespace Denbot.Ingest.Commands {
    public class SlashUnhomieModule : ApplicationCommandsModule {
        [SlashCommandGroup("unhomie", "Starts unhomie votes and configures the voting system")]
        public class Unhomie : UnhomieModuleBase {
            public Unhomie(IRemoveRoleVoteService removeRoleVoteService, ISchedulerFactory schedulerFactory) : base(
                removeRoleVoteService, schedulerFactory) {
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
                long timeout = 10,
                [Option("backfire", "Determines if a failed vote will unhomie the initiating user")]
                bool isBackfireEnabled = true,
                [Option("selfx", "Determines if the target user voting nay will result in a immediate unhomie")]
                bool isSelfXEnabled = true) {
                var interactionResponseBuilder = new DiscordInteractionResponseBuilder()
                    .AsEphemeral(true);
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    interactionResponseBuilder);
                var msgBuilder = new DiscordWebhookBuilder();

                var settings = await RemoveRoleVoteService.CreateOrUpdateGuildRemoveRoleSettingsAsync(context.Guild.Id,
                    context.Guild.OwnerId, new RemoveRoleSettings {
                        Period = Convert.ToInt32(period),
                        Quorum = Convert.ToInt32(quorum),
                        Timeout = Convert.ToInt32(timeout),
                        IsEnabled = isEnabled,
                        TargetableRole = role.Id,
                        IsBackfireEnabled = isBackfireEnabled
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
                await StartVoteAsync(context, target);
            }

            [SlashIsOwner]
            [SlashCommand("override_restore", "Overrides the role restoration process")]
            public async Task OverrideRestoreAsync(InteractionContext ctx,
                [Option("target", "User to restore")] DiscordUser target) {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true));
                var memberTarget = await ctx.Guild.GetMemberAsync(target.Id);
                var guildSettings = await RemoveRoleVoteService.GetGuildRemoveRoleSettingsAsync(ctx.Guild.Id);
                var targetRole = ctx.Guild.GetRole(guildSettings.Value.TargetableRole);
                await memberTarget.GrantRoleAsync(targetRole,
                    "DenBot: Manual restore process override, should only be used if the bot fails");
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent(
                        $"User {target.Mention} has been restored to role {targetRole.Mention}"));
            }
        }
    }
}