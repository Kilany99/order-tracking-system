
using NotificationService.Domain.Interfaces;
using System.Threading.Channels;

namespace NotificationService.Infrastructure.Channels;


public class NotificationChannel<T> : INotificationChannel<T>
{
    private readonly Channel<T> _channel;

    public NotificationChannel()
    {
        _channel = Channel.CreateUnbounded<T>();
    }

    public ChannelWriter<T> Writer => _channel.Writer;
    public ChannelReader<T> Reader => _channel.Reader;
}