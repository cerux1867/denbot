using System.Linq;
using System.Threading.Tasks;
using Denbot.Common.Models;
using Denbot.Ingest.Results;
using Denbot.Ingest.Services;
using DisCatSharp;
using DisCatSharp.Entities;

namespace Denbot.Ingest.InteractionHandlers {
    public class RoleRemovalBallotHandler : InteractionHandlerBase {
        private readonly IRemoveRoleVoteService _removeRoleVoteService;

        public RoleRemovalBallotHandler(DiscordClient client, IRemoveRoleVoteService removeRoleVoteService) : base(client) {
            _removeRoleVoteService = removeRoleVoteService;
        }
        
        public override async Task HandleAsync(DiscordInteraction interaction) {
            await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var ballotIdentifierComponents = interaction.Data.CustomId.Split("-");
            var ongoingVote = await _removeRoleVoteService.GetVoteAsync(ballotIdentifierComponents[1]);
            if (ongoingVote.Value.State != VoteState.Ongoing) {
                await interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent("Error: The vote has finished")
                        .AsEphemeral(true));
                return;
            }

            var userBallot = ongoingVote.Value.Ballots.FirstOrDefault(b => b.VoterId == interaction.User.Id);
            var type = ballotIdentifierComponents[2] == "aye" ? BallotType.Aye : BallotType.Nay;
            if (userBallot != null) {
                if (type != userBallot.Type) {
                    var changeBallot =
                        await _removeRoleVoteService.CastBallotAsync(ballotIdentifierComponents[1], type, interaction.User.Id);
                    await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"You have already cast a vote - **{userBallot.Type}**, it has been changed to **{type}**")
                        .AsEphemeral(true));
                }
            }
            else {
                var castBallotResult =
                    await _removeRoleVoteService.CastBallotAsync(ballotIdentifierComponents[1], type,
                        interaction.User.Id);
                if (castBallotResult.Type != ResultType.Ok) {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            "Error: There was an issue processing your submission")
                            .AsEphemeral(true));
                    return;
                }

                await interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Your vote has been processed and submitted").AsEphemeral(true));
            }
        }
    }
}