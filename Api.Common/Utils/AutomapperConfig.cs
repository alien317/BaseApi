using Api.Common.Models.DTOs.Core;
using Api.Data.Models.Core;
using AutoMapper;

namespace Api.Common.Utils
{
    public class AutomapperConfig : Profile
    {
        public AutomapperConfig()
        {
            var entityDtoMappings = new Dictionary<Type, Type>
            {
                { typeof(ApplicationUser), typeof(UserDTO) },
                { typeof(Role), typeof(RoleDTO) },
                { typeof(Transaction), typeof(TransactionDTO) },                
            };

            foreach (var mapping in entityDtoMappings)
            {
                CreateMap(mapping.Key, mapping.Value).ReverseMap()
                    .ForMember("Id", opt => opt.Ignore())
                    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            }         
        }
    }
}
