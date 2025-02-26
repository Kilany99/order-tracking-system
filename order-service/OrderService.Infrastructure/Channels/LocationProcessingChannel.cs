using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;

using System.Threading.Channels;

namespace OrderService.Infrastructure.Channels;


public class LocationProcessingChannel : IOrderProcessingChannel<DriverLocationEvent>
{
    private readonly Channel<DriverLocationEvent> _channel;

    public LocationProcessingChannel()
    {
        _channel = Channel.CreateUnbounded<DriverLocationEvent>();
    }

    public ChannelWriter<DriverLocationEvent> Writer => _channel.Writer;
    public ChannelReader<DriverLocationEvent> Reader => _channel.Reader;
}
