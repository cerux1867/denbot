using System.Threading.Tasks;
using DSharpPlusNextGen;
using DSharpPlusNextGen.Entities;

namespace Denbot.Ingest.InteractionHandlers {
    public abstract class InteractionHandlerBase : IInteractionHandler {
        protected readonly DiscordClient Client;

        protected InteractionHandlerBase(DiscordClient client) {
            Client = client;
        }
        public abstract Task HandleAsync(DiscordInteraction interaction);
    }
}