using System.Threading.Channels;
using WalletApi.Application.Abstractions;

namespace WalletApi.Infrastructure.Messaging;

public class InMemorySubmissionQueue : IPaymentSubmissionQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ChannelReader<string> Reader => _channel.Reader;

    public void Enqueue(string operationId) => _channel.Writer.TryWrite(operationId);
}
