using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
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
                .WithContent($"User {member.Mention} has been restored to the role **{role.Name}**"));
        }
    }
}