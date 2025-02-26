using MediatR;
using OrderService.Domain.Models;


namespace OrderService.Application.Features.Orders.Queries;


public record GetCustomerInfoQuery(string CustomerId) : IRequest<CustomerInfoResponse>;


