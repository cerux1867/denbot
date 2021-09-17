using System.Linq;
using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;

namespace Denbot.Ingest.Attributes {
    public class SlashIsOwnerAttribute : SlashCheckBaseAttribute {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
            var currentApplication = ctx.Client.CurrentApplication;
            var currentUser = ctx.Client.CurrentUser;
            return !(currentApplication != null)
                ? Task.FromResult((long) ctx.User.Id == (long) currentUser.Id)
                : Task.FromResult(
                    currentApplication.Owners.Any(x => (long) x.Id == (long) ctx.User.Id));
        }
    }
}