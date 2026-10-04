namespace WhatsAppDemo.WhatsApp;

public interface IWhatsAppSender
{
    bool IsConfigured { get; }

    /// <summary>Número remitente, p. ej. "whatsapp:+14155238886".</summary>
    string From { get; }

    /// <param name="phone">Número del cliente en formato E.164, p. ej. "+573001234567".</param>
    Task<SendResult> SendAsync(string phone, string text, CancellationToken cancellationToken);
}

public sealed record SendResult(bool Success, string? Error = null)
{
    public static SendResult Ok() => new(true);

    public static SendResult Failed(string error) => new(false, error);
}
