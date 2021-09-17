using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;

namespace Denbot.Ingest.Attributes {
    /// <summary>
    /// An implementation of RequireUserPermissionsAttribute for slash commands.
    /// Defines that usage of this command is restricted to members with specified permissions.
    /// </summary>
    public class SlashRequireUserPermissionsAttribute : SlashCheckBaseAttribute {
        public Permissions Permissions { get; }
        public bool IgnoreDms { get; } = true;

        public SlashRequireUserPermissionsAttribute(Permissions permissions) {
            Permissions = permissions;
        }

        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
            if (ctx.Guild == null) {
                return Task.FromResult(IgnoreDms);
            }

            var member = ctx.Member;
            if (member == null) {
                return Task.FromResult(false);
            }

            if ((long) member.Id == (long) ctx.Guild.OwnerId) {
                return Task.FromResult(true);
            }

            var permissions = ctx.Channel.PermissionsFor(member);
            if ((permissions & Permissions.Administrator) != Permissions.None) {
                return Task.FromResult(true);
            }

            return (permissions & Permissions) != Permissions ? Task.FromResult(false) : Task.FromResult(true);
        }
    }
}