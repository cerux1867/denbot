using System;
using System.Threading.Tasks;
using Denbot.Ingest.Services;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Enums;
using Quartz;

namespace Denbot.Ingest.Commands {
    public class ContextMenuUnhomieModule : UnhomieModuleBase {
        public ContextMenuUnhomieModule(IRemoveRoleVoteService removeRoleVoteService, ISchedulerFactory schedulerFactory) : base(removeRoleVoteService, schedulerFactory) {
        }

        [ContextMenu(ApplicationCommandType.User, "Start unhomie vote")]
        public async Task VoteAsync(ContextMenuContext ctx) {
            await StartVoteAsync(ctx, ctx.TargetUser);
        }
    }
}