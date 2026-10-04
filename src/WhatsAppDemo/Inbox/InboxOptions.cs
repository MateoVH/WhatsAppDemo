namespace WhatsAppDemo.Inbox;

public sealed class InboxOptions
{
    public const string Section = "Inbox";

    /// <summary>
    /// La bandeja no tiene login: por defecto solo responde a peticiones locales.
    /// Lo que llega por un túnel (ngrok, dev tunnels) solo puede usar el webhook.
    /// </summary>
    public bool LocalOnly { get; set; } = true;

    /// <summary>Cuántos mensajes recientes recibe la IA como contexto.</summary>
    public int HistoryLimit { get; set; } = 20;
}
