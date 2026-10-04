using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.WhatsApp;

/// <summary>Webhook "When a message comes in" del sandbox de WhatsApp de Twilio.</summary>
public static class TwilioWebhook
{
    public const string Path = "/webhooks/twilio";

    public static IEndpointRouteBuilder MapTwilioWebhook(this IEndpointRouteBuilder app)
    {
        // El formulario se lee a mano porque la firma se calcula sobre todos sus campos.
        app.MapPost(Path, HandleAsync).DisableAntiforgery();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request, TwilioSignatureValidator signatures, InboxService inbox, ILogger<InboxService> logger)
    {
        if (!request.HasFormContentType)
        {
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
        if (!signatures.IsValid(request, form))
        {
            logger.LogWarning("Webhook rechazado: firma de Twilio inválida. ¿Coincide la URL pública con la configurada en Twilio?");
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var from = form["From"].ToString();
        if (from.Length == 0)
        {
            return Results.BadRequest();
        }

        await inbox.ReceiveAsync(new IncomingMessage(
            Phone: from.Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase),
            Name: form["ProfileName"].ToString(),
            Text: DescribeContent(form),
            ExternalId: form["MessageSid"].ToString(),
            IsSimulated: false));

        // TwiML vacío: la respuesta de la IA sale después por la API REST, así el webhook contesta
        // en milisegundos y nunca se acerca al timeout de 15 s de Twilio.
        return Results.Content("<Response/>", "application/xml");
    }

    private static string DescribeContent(IFormCollection form)
    {
        var body = form["Body"].ToString().Trim();
        if (body.Length > 0)
        {
            return body;
        }

        // Bilingüe a propósito: lo leen tanto la bandeja (en cualquier idioma) como la IA.
        return int.TryParse(form["NumMedia"], out var media) && media > 0
            ? "📎 (adjunto · attachment)"
            : "(sin texto · no text)";
    }
}
