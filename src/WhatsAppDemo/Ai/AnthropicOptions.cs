namespace WhatsAppDemo.Ai;

public sealed class AnthropicOptions
{
    public const string Section = "Anthropic";

    /// <summary>Va en user-secrets o variables de entorno, nunca en appsettings.json. También se acepta ANTHROPIC_API_KEY.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    /// <summary>low | medium | high | xhigh | max. En un chat, "low" da respuestas rápidas y de buena calidad.</summary>
    public string Effort { get; set; } = "low";

    /// <summary>Tope de tokens por respuesta (incluye el razonamiento interno del modelo).</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>Instrucciones del asistente, relativas a la raíz del proyecto.</summary>
    public string SystemPromptFile { get; set; } = "Prompts/system-prompt.md";

    /// <summary>Zona horaria del negocio: el prompt incluye la hora local para responder "¿están abiertos?".</summary>
    public string TimeZone { get; set; } = "America/Bogota";
}
