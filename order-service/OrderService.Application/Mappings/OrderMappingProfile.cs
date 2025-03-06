using AutoMapper;
using OrderService.Application.Responses;
using OrderService.Domain.Entities;
using OrderService.Domain.Models;


namespace OrderService.Application.Mappings;


public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.DeliveryAddress, opt => opt.MapFrom(src => src.DeliveryAddress))
            .ForMember(dest => dest.DeliveryLatitude, opt => opt.MapFrom(src => src.DeliveryLatitude))
            .ForMember(dest => dest.DeliveryLongitude, opt => opt.MapFrom(src => src.DeliveryLongitude))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.DriverId, opt => opt.MapFrom(src => src.DriverId))
            .ForMember(dest => dest.DriverDetails, opt => opt.MapFrom(src => src.DriverDetails));

        CreateMap<DriverDetails, DriverDetailsDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.CurrentLatitude, opt => opt.MapFrom(src => src.CurrentLatitude))
            .ForMember(dest => dest.CurrentLongitude, opt => opt.MapFrom(src => src.CurrentLongitude));
    }
}