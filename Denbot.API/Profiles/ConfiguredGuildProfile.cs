using AutoMapper;
using Denbot.API.Models;
using Denbot.Common.Entities;
using Denbot.Common.Models;

namespace Denbot.API.Profiles {
    public class ConfiguredGuildProfile : Profile {
        public ConfiguredGuildProfile() {
            CreateMap<ConfiguredGuildEntity, ConfiguredGuildDto>();
        }
    }
}