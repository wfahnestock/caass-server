using AutoMapper;
using CAASS.Auth.Models.Api.Dto;

namespace CAASS.Auth.Models.Entities.Mappings;

public class TenantContactMappingProfile : Profile
{
    public TenantContactMappingProfile()
    {
        CreateMap<TenantContactDto, TenantContact>()
            // Map ContactType enum to boolean flags
            .ForMember(dest => dest.IsPrimaryContact, opt => opt.MapFrom(src => src.ContactType == TenantContactType.Primary))
            .ForMember(dest => dest.IsBillingContact, opt => opt.MapFrom(src => src.ContactType == TenantContactType.Billing))
            .ForMember(dest => dest.IsTechnicalContact, opt => opt.MapFrom(src => src.ContactType == TenantContactType.Technical))
            // Handle nullable properties
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? string.Empty))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title ?? string.Empty))
            // Map TenantId from context
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom((src, dest, destMember, context) => 
                context.Items.TryGetValue("TenantId", out object? item) ? (Guid)item : Guid.Empty))
            // Ignore properties that need to be set separately
            .ForMember(dest => dest.TenantContactId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            // Map Address fields
            .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.StreetAddress))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.ZipCode))
            .ForMember(dest => dest.CountryCode, opt => opt.Ignore());
    }
}