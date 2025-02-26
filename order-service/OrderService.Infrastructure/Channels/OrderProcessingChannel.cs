using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using System.Threading.Channels;

namespace OrderService.Infrastructure.Channels;

public class OrderProcessingChannel : IOrderProcessingChannel
{
    private readonly Channel<OrderCreatedEvent> _channel;

    public OrderProcessingChannel()
    {
        _channel = Channel.CreateUnbounded<OrderCreatedEvent>();
    }

    public ChannelWriter<OrderCreatedEvent> Writer => _channel.Writer;
    public ChannelReader<OrderCreatedEvent> Reader => _channel.Reader;
}