using OrderService.Domain.Models;
using System.Threading.Channels;

namespace OrderService.Domain.Interfaces;

public interface IOrderProcessingChannel
{
    ChannelWriter<OrderCreatedEvent> Writer { get; }
    ChannelReader<OrderCreatedEvent> Reader { get; }
}