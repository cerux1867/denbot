﻿using System;
using System.Threading.Tasks;
using DSharpPlusNextGen.Entities;
using DSharpPlusNextGen.SlashCommands;
using Quartz;

namespace Denbot.Ingest.Jobs {
    public class RoleRestoreJob : IJob {
        public async Task Execute(IJobExecutionContext context) {
            var targetRole = (DiscordRole) context.MergedJobDataMap["targetRole"];
            var targetUser = (DiscordUser) context.MergedJobDataMap["targetUser"];
            var interactionContext = (InteractionContext) context.MergedJobDataMap["interactionContext"];
            var role = interactionContext.Guild.GetRole(targetRole.Id);
            var member  = await interactionContext.Guild.GetMemberAsync(targetUser.Id);
            await member.GrantRoleAsync(role, "Denbot: Role restore after vote");
            await interactionContext.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You have been restored to the role {role.Mention}")
                .AsEphemeral(true));
        }
    }
}