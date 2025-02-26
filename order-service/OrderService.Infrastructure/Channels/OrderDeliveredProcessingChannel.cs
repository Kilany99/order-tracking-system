using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;

using System.Threading.Channels;
namespace OrderService.Infrastructure.Channels;

public class OrderDeliveredProcessingChannel : IOrderProcessingChannel<OrderDeliveredEvent>
{
    private readonly Channel<OrderDeliveredEvent> _channel;

    public OrderDeliveredProcessingChannel()
    {
        _channel = Channel.CreateUnbounded<OrderDeliveredEvent>();
    }

    public ChannelWriter<OrderDeliveredEvent> Writer => _channel.Writer;
    public ChannelReader<OrderDeliveredEvent> Reader => _channel.Reader;
}