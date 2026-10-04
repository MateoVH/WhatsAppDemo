using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Ai;

/// <summary>Genera la respuesta a una conversación. Aislar el proveedor aquí permite cambiar de modelo sin tocar el resto.</summary>
public interface IReplyGenerator
{
    /// <summary>Nombre visible en la bandeja, p. ej. "Claude · claude-opus-5-5".</summary>
    string Name { get; }

    /// <summary>False cuando las respuestas son simuladas (no hay API key).</summary>
    bool IsLive { get; }

    /// <exception cref="ReplyGenerationException">El modelo no pudo responder.</exception>
    Task<string> GenerateReplyAsync(IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken);
}

/// <summary>Por qué no hubo respuesta. La bandeja lo muestra traducido; el mensaje de la excepción queda para los logs.</summary>
public enum ReplyFailure { InvalidApiKey, ModelNotFound, RateLimited, Unavailable, Network, ApiError, Refused, EmptyReply, Unexpected }

public sealed class ReplyGenerationException(ReplyFailure reason, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ReplyFailure Reason { get; } = reason;
}
