using System.Threading.Tasks;
using DSharpPlusNextGen.Entities;

namespace Denbot.Ingest.InteractionHandlers {
    public interface IInteractionHandler {
        public Task HandleAsync(DiscordInteraction interaction);
    }
}