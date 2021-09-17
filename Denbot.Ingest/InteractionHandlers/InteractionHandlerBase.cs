using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;

namespace Denbot.Ingest.InteractionHandlers {
    public abstract class InteractionHandlerBase : IInteractionHandler {
        protected readonly DiscordClient Client;

        protected InteractionHandlerBase(DiscordClient client) {
            Client = client;
        }
        public abstract Task HandleAsync(DiscordInteraction interaction);
    }
}