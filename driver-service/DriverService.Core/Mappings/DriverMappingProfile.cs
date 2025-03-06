using AutoMapper;
using DriverService.Domain.Entities;
using DriverService.Domain.Models;


namespace DriverService.Core.Mappings;


public class DriverMappingProfile : Profile
{
    public DriverMappingProfile()
    {
        CreateMap<Driver, DriverDetailsDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CurrentLatitude, opt => opt.MapFrom(src => src.CurrentLatitude))
            .ForMember(dest => dest.CurrentLongitude, opt => opt.MapFrom(src => src.CurrentLongitude));
    }
}