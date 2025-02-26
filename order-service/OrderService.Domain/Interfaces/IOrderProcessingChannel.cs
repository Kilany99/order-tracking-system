using OrderService.Domain.Models;
using System.Threading.Channels;

namespace OrderService.Domain.Interfaces;

public interface IOrderProcessingChannel<T> where T : class
{
    ChannelWriter<T> Writer { get; }
    ChannelReader<T> Reader { get; }
}