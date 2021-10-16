using System.Threading.Tasks;
using AutoMapper;
using Denbot.API.Services;
using Denbot.Common.Entities;
using Denbot.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Denbot.API.Controllers {
    [Route("[controller]")]
    [ApiController]
    public class GuildsController : ControllerBase {
        private readonly IGuildsService _guildsService;
        private readonly IMapper _mapper;

        public GuildsController(IGuildsService guildsService, IMapper mapper) {
            _guildsService = guildsService;
            _mapper = mapper;
        }
        
        [HttpGet("{guildId}/Settings")]
        public async Task<ActionResult<GuildSettings>> GetSettingsByIdAsync([FromRoute] ulong guildId) {
            var guild = await _guildsService.GetByIdAsync(guildId);
            if (guild == null) {
                return NotFound(new { error = $"Guild with ID {guildId} was not found" });
            }
            return Ok(guild.Settings);
        }
        
        [HttpGet("{guildId}")]
        public async Task<ActionResult<ConfiguredGuildDto>> GetByIdAsync([FromRoute] ulong guildId) {
            var guild = await _guildsService.GetByIdAsync(guildId);
            if (guild == null) {
                return NotFound(new { error = $"Guild with ID {guildId} was not found" });
            }
            return Ok(_mapper.Map<ConfiguredGuildDto>(guild));
        }

        [HttpPost]
        public async Task<ActionResult<ConfiguredGuildDto>> AddAsync([FromBody] CreatableGuild guild) {
            var result = await _guildsService.CreateAsync(new ConfiguredGuildEntity {
                GuildId= guild.Id,
                Settings = new GuildSettings {
                    RemoveRoleSettings = guild.RemoveRoleSettings
                }
            });
            return Ok(_mapper.Map<ConfiguredGuildDto>(result));
        } 
    }
}