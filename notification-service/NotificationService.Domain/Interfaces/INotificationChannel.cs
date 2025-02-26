
using System.Threading.Channels;

namespace NotificationService.Domain.Interfaces;


public interface INotificationChannel<T>
{
    ChannelWriter<T> Writer { get; }
    ChannelReader<T> Reader { get; }
}