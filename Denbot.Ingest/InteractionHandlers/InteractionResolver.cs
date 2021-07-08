using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlusNextGen.Entities;

namespace Denbot.Ingest.InteractionHandlers {
    public class InteractionResolver {
        private readonly Dictionary<string, IInteractionHandler> _handlerDictionary;
        
        public InteractionResolver(RoleRemovalBallotHandler roleRemovalBallotHandler) {
            _handlerDictionary = new Dictionary<string, IInteractionHandler> {
                {"RoleRemovalBallot", roleRemovalBallotHandler}
            };
        }

        public async Task ResolveInteractionAsync(DiscordInteraction interaction) {
            if (!string.IsNullOrEmpty(interaction.Data.CustomId)) {
                var interactionIdentifierComponents = interaction.Data.CustomId.Split("-");
                var interactionId = interactionIdentifierComponents[0];
                _handlerDictionary.TryGetValue(interactionId, out var handler);
                if (handler != null) {
                    await handler.HandleAsync(interaction);
                }
            }
        }
    }
}