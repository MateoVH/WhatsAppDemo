using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Ai;

/// <summary>Respuestas simuladas para probar la bandeja sin API key.</summary>
public sealed class DemoReplyGenerator : IReplyGenerator
{
    private static readonly HashSet<string> SpanishWords =
        ["hola", "buenas", "gracias", "quiero", "tienen", "hacen", "por", "favor", "para", "con", "una", "que", "como", "cuanto", "el", "la", "los", "las", "de", "y", "hay"];

    public string Name => "Demo";

    public bool IsLive => false;

    public async Task<string> GenerateReplyAsync(IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken)
    {
        // Simula la latencia del modelo para que se vea el indicador "escribiendo…".
        await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);

        var text = history[^1].Text;
        return LooksSpanish(text)
            ? $"🤖 Modo demo: recibí «{text}». Configura Anthropic:ApiKey para que responda Claude."
            : $"🤖 Demo mode: I got “{text}”. Set Anthropic:ApiKey to get replies from Claude.";
    }

    // Heurística mínima para imitar a Claude, que responde en el idioma del cliente.
    private static bool LooksSpanish(string text) =>
        text.IndexOfAny(['¿', '¡', 'ñ', 'á', 'é', 'í', 'ó', 'ú']) >= 0
        || text.ToLowerInvariant().Split([' ', ',', '.', '?', '!'], StringSplitOptions.RemoveEmptyEntries).Any(SpanishWords.Contains);
}
