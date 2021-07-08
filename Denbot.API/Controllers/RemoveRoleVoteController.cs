using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Denbot.API.Entities;
using Denbot.API.Models;
using Denbot.API.Services;
using Denbot.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Denbot.API.Controllers {
    [Route("Remove-Role-Votes")]
    [ApiController]
    public class RemoveRoleVoteController : ControllerBase {
        private readonly IRemoveRoleVoteService _removeRoleVoteService;

        public RemoveRoleVoteController(IRemoveRoleVoteService removeRoleVoteService) {
            _removeRoleVoteService = removeRoleVoteService;
        }

        [HttpGet("~/Guilds/{guildId}/Remove-Role-Votes")]
        public async Task<ActionResult<List<RemoveRoleVoteEntity>>> GetAllAsync([FromRoute] ulong guildId, [FromQuery] VoteState? state) {
            var response = await _removeRoleVoteService.GetAllByGuildAsync(guildId, state);
            return Ok(response);
        }

        [HttpPost("~/Guilds/{guildId}/Remove-Role-Votes")]
        public async Task<ActionResult<RemoveRoleVoteEntity>> AddAsync([FromRoute] ulong guildId, [FromBody] CreatableRemoveRoleVote vote) {
            var response = await _removeRoleVoteService.CreateInGuildAsync(guildId, new RemoveRoleVoteEntity {
                Ballots = new List<RemoveRoleBallot>(),
                State = VoteState.Ongoing,
                ExpiresAt = vote.ExpiresAt,
                GuildId = guildId,
                StartedAt = DateTimeOffset.Now,
                InitiatingUserId = vote.InitiatingUserId,
                LastUpdatedAt = DateTimeOffset.Now,
                TargetUserId = vote.TargetUserId
            });
            if (response == null) {
                return Conflict();
            }

            return CreatedAtAction("GetById", new {voteId = response.VoteId}, response);
        }
        
        [HttpGet("{voteId}")]
        public async Task<ActionResult<RemoveRoleVoteEntity>> GetByIdAsync([FromRoute] string voteId) {
            var response = await _removeRoleVoteService.GetByIdAsync(voteId);
            if (response == null) {
                return NotFound();
            }

            return response;
        }
        
        [HttpPost("{voteId}/Ballots")]
        public async Task<ActionResult<List<RemoveRoleVoteEntity>>> AddBallotAsync([FromRoute] string voteId, [FromBody] RemoveRoleBallot ballot) {
            var response = await _removeRoleVoteService.AddBallotAsync(voteId, ballot);
            if (response == null) {
                return NotFound();
            }

            return Ok(response);
        }
    }
}