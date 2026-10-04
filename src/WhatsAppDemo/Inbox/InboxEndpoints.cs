using System.ComponentModel.DataAnnotations;
using System.Net;
using WhatsAppDemo.Ai;
using WhatsAppDemo.WhatsApp;

namespace WhatsAppDemo.Inbox;

public static class InboxEndpoints
{
    public static IEndpointRouteBuilder MapInboxApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/status", (IReplyGenerator ai, IWhatsAppSender whatsApp) => new StatusResponse(
            new(ai.Name, ai.IsLive),
            new(whatsApp.From.Replace("whatsapp:", ""), whatsApp.IsConfigured),
            TwilioWebhook.Path));

        api.MapGet("/conversations", (InboxStore store) => store.GetConversations());

        api.MapGet("/conversations/{id}", (string id, InboxStore store) =>
            store.GetConversation(id) is { } conversation ? Results.Ok(conversation) : Results.NotFound());

        api.MapPost("/conversations/{id}/messages", async (string id, SendMessageRequest request, InboxService inbox, CancellationToken ct) =>
            await inbox.SendAgentReplyAsync(id, request.Text.Trim(), ct) is { } message ? Results.Ok(message) : Results.NotFound());

        api.MapPut("/conversations/{id}/auto-reply", async (string id, AutoReplyRequest request, InboxService inbox) =>
            await inbox.SetAutoReplyAsync(id, request.Enabled) is { } conversation ? Results.Ok(conversation) : Results.NotFound());

        // Permite probar el flujo completo sin teléfono: entra igual que un mensaje de Twilio, pero nunca sale por WhatsApp.
        api.MapPost("/simulator/messages", async (SimulatedMessageRequest request, InboxService inbox) =>
        {
            await inbox.ReceiveAsync(new IncomingMessage(request.Phone, request.Name ?? "", request.Text.Trim(), ExternalId: null, IsSimulated: true));
            return Results.Accepted();
        });

        return app;
    }

    /// <summary>
    /// Bloquea todo menos el webhook para peticiones que no vengan de esta máquina.
    /// Debe ir después de UseForwardedHeaders para ver la IP real de quien llega por el túnel.
    /// </summary>
    public static IApplicationBuilder UseLocalOnlyInbox(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (IsLocal(context) || context.Request.Path.StartsWithSegments(TwilioWebhook.Path))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync(
            "La bandeja solo está disponible desde localhost · The inbox is only available from localhost (Inbox:LocalOnly).");
    });

    private static bool IsLocal(HttpContext context)
    {
        // Un túnel siempre deja rastro en X-Forwarded-* (o X-Original-* si ya se procesaron).
        if (context.Request.Headers.Keys.Any(h => h.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase)
                                               || h.StartsWith("X-Original-", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Sin IP remota la petición es en proceso (p. ej. TestServer).
        var ip = context.Connection.RemoteIpAddress;
        return ip is null || IPAddress.IsLoopback(ip) || (ip.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(ip.MapToIPv4()));
    }
}

/// <summary>Estado de las integraciones. La bandeja arma las etiquetas en su idioma.</summary>
public sealed record StatusResponse(AiStatus Ai, WhatsAppStatus WhatsApp, string WebhookPath);

public sealed record AiStatus(string Name, bool IsLive);

public sealed record WhatsAppStatus(string Number, bool IsLive);

public sealed record SendMessageRequest([Required, StringLength(1600)] string Text);

public sealed record AutoReplyRequest(bool Enabled);

public sealed record SimulatedMessageRequest(
    [Required, RegularExpression(@"^\+?[0-9 ()-]{7,20}$")] string Phone,
    [StringLength(60)] string? Name,
    [Required, StringLength(1600)] string Text);
