using System.Threading.Tasks;
using DisCatSharp.Entities;

namespace Denbot.Ingest.InteractionHandlers {
    public interface IInteractionHandler {
        public Task HandleAsync(DiscordInteraction interaction);
    }
}