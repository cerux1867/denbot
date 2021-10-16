using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Denbot.API.Services;
using Denbot.Common.Entities;
using Denbot.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Denbot.API.Controllers {
    [Route("Remove-Role-Votes")]
    [ApiController]
    public class RemoveRoleVoteController : ControllerBase {
        private readonly IRemoveRoleVoteService _removeRoleVoteService;
        private readonly IMapper _mapper;

        public RemoveRoleVoteController(IRemoveRoleVoteService removeRoleVoteService, IMapper mapper) {
            _removeRoleVoteService = removeRoleVoteService;
            _mapper = mapper;
        }

        [HttpGet("~/Guilds/{guildId}/Remove-Role-Votes")]
        public async Task<ActionResult<List<RemoveRoleVoteDto>>> GetAllAsync([FromRoute] ulong guildId, [FromQuery] VoteState? state) {
            var response = await _removeRoleVoteService.GetAllByGuildAsync(guildId, state);
            return Ok(_mapper.Map<List<RemoveRoleVoteDto>>(response));
        }

        [HttpPost("~/Guilds/{guildId}/Remove-Role-Votes")]
        public async Task<ActionResult<RemoveRoleVoteDto>> AddAsync([FromRoute] ulong guildId, [FromBody] CreatableRemoveRoleVote vote) {
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

            return CreatedAtAction("GetById", new {voteId = response.Id}, 
                _mapper.Map<RemoveRoleVoteDto>(response));
        }
        
        [HttpGet("{voteId}")]
        public async Task<ActionResult<RemoveRoleVoteDto>> GetByIdAsync([FromRoute] string voteId) {
            var response = await _removeRoleVoteService.GetByIdAsync(voteId);
            if (response == null) {
                return NotFound();
            }

            return Ok(_mapper.Map<RemoveRoleVoteDto>(response));
        }
        
        [HttpPost("{voteId}/Ballots")]
        public async Task<ActionResult<RemoveRoleVoteDto>> AddBallotAsync([FromRoute] string voteId, [FromBody] RemoveRoleBallot ballot) {
            var response = await _removeRoleVoteService.AddBallotAsync(voteId, ballot);
            if (response == null) {
                return NotFound();
            }

            return Ok(_mapper.Map<RemoveRoleVoteDto>(response));
        }
    }
}