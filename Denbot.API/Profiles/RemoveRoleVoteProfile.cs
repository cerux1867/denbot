using AutoMapper;
using Denbot.Common.Entities;
using Denbot.Common.Models;

namespace Denbot.API.Profiles {
    public class RemoveRoleVoteProfile : Profile {
        public RemoveRoleVoteProfile() {
            CreateMap<RemoveRoleVoteEntity, RemoveRoleVoteDto>();
        }
    }
}