using System.Threading.Tasks;
using DSharpPlusNextGen;
using DSharpPlusNextGen.SlashCommands;

namespace Denbot.Ingest.Attributes {
    public class SlashIsOwnerOrRequireUserPermissionAttribute : SlashCheckBaseAttribute {
        public Permissions Permissions { get; }

        public SlashIsOwnerOrRequireUserPermissionAttribute(Permissions permissions) {
            Permissions = permissions;
        }
        
        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
            var isOwner = await new SlashIsOwnerAttribute().ExecuteChecksAsync(ctx);
            var hasPermission = await new SlashRequireUserPermissionsAttribute(Permissions).ExecuteChecksAsync(ctx);
            return isOwner || hasPermission;
        }
    }
}