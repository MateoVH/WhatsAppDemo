namespace WhatsAppDemo.WhatsApp;

public sealed class TwilioOptions
{
    public const string Section = "Twilio";

    /// <summary>Credenciales: van en user-secrets o variables de entorno, nunca en appsettings.json.</summary>
    public string? AccountSid { get; set; }

    public string? AuthToken { get; set; }

    /// <summary>Remitente de las respuestas. Por defecto, el número del sandbox de WhatsApp de Twilio.</summary>
    public string WhatsAppFrom { get; set; } = "whatsapp:+14155238886";

    /// <summary>
    /// URL pública exacta configurada en Twilio. Opcional: si se omite se reconstruye desde la petición,
    /// lo que funciona con túneles que envían X-Forwarded-Host/Proto (ngrok, dev tunnels).
    /// </summary>
    public string? WebhookUrl { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken);
}
