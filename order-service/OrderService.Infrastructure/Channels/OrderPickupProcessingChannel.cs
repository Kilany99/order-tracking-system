using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using System.Threading.Channels;

namespace OrderService.Infrastructure.Channels;

public class OrderPickupProcessingChannel : IOrderProcessingChannel<OrderPickedUpEvent>
{
    private readonly Channel<OrderPickedUpEvent> _channel;

    public OrderPickupProcessingChannel()
    {
        _channel = Channel.CreateUnbounded<OrderPickedUpEvent>();
    }

    public ChannelWriter<OrderPickedUpEvent> Writer => _channel.Writer;
    public ChannelReader<OrderPickedUpEvent> Reader => _channel.Reader;
}