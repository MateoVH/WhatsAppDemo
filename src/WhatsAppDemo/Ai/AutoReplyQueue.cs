using System.Threading.Channels;

namespace WhatsAppDemo.Ai;

/// <summary>Conversaciones que esperan respuesta de la IA. Encolar dos veces la misma no es problema.</summary>
public sealed class AutoReplyQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new() { SingleReader = true });

    public void Enqueue(string conversationId) => _channel.Writer.TryWrite(conversationId);

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
