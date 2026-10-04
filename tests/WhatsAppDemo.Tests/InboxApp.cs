using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppDemo.Ai;
using WhatsAppDemo.Inbox;
using WhatsAppDemo.WhatsApp;

namespace WhatsAppDemo.Tests;

/// <summary>La app real, con la IA y WhatsApp reemplazados por dobles de prueba: nada sale a Internet.</summary>
public sealed class InboxApp : WebApplicationFactory<Program>
{
    public const string AuthToken = "test-auth-token";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public FakeReplyGenerator Ai { get; } = new();

    public FakeWhatsAppSender WhatsApp { get; } = new();

    public static Dictionary<string, string> WhatsAppMessage(string phone, string body, string? messageSid = null) => new()
    {
        ["MessageSid"] = messageSid ?? $"SM{Guid.NewGuid():N}",
        ["From"] = $"whatsapp:{phone}",
        ["To"] = "whatsapp:+14155238886",
        ["Body"] = body,
        ["ProfileName"] = "Ana",
        ["WaId"] = phone.TrimStart('+'),
        ["NumMedia"] = "0",
    };

    /// <summary>Envía un webhook firmado como lo firma Twilio (o con la firma indicada).</summary>
    public async Task<HttpResponseMessage> PostWebhookAsync(
        Dictionary<string, string> form, string? signature = null, string? forwardedFor = null)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, TwilioWebhook.Path) { Content = new FormUrlEncodedContent(form) };
        request.Headers.Add("X-Twilio-Signature", signature ?? Sign(new Uri(client.BaseAddress!, TwilioWebhook.Path).ToString(), form));
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return await client.SendAsync(request);
    }

    /// <summary>Espera a que la conversación cumpla la condición: la IA responde en segundo plano.</summary>
    public async Task<ConversationDetail> WaitForConversationAsync(string id, Func<ConversationDetail, bool> condition)
    {
        var client = CreateClient();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var response = await client.GetAsync($"/api/conversations/{id}");
            if (response.IsSuccessStatusCode
                && await response.Content.ReadFromJsonAsync<ConversationDetail>(Json) is { } conversation
                && condition(conversation))
            {
                return conversation;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"La conversación {id} no llegó al estado esperado.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Twilio:AccountSid", "ACtest");
        builder.UseSetting("Twilio:AuthToken", AuthToken);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IReplyGenerator>(Ai);
            services.AddSingleton<IWhatsAppSender>(WhatsApp);
        });
    }

    // Algoritmo de Twilio: URL + parámetros ordenados (clave+valor), HMAC-SHA1 con el Auth Token, en Base64.
    private static string Sign(string url, Dictionary<string, string> form)
    {
        var data = new StringBuilder(url);
        foreach (var (key, value) in form.OrderBy(field => field.Key, StringComparer.Ordinal))
        {
            data.Append(key).Append(value);
        }

        return Convert.ToBase64String(HMACSHA1.HashData(Encoding.UTF8.GetBytes(AuthToken), Encoding.UTF8.GetBytes(data.ToString())));
    }
}

public sealed class FakeReplyGenerator : IReplyGenerator
{
    public string Name => "IA de prueba";

    public bool IsLive => true;

    public List<IReadOnlyList<ChatTurn>> Requests { get; } = [];

    /// <summary>Si se asigna, simula que el modelo falla por ese motivo.</summary>
    public ReplyFailure? Failure { get; set; }

    public Task<string> GenerateReplyAsync(IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken)
    {
        Requests.Add(history);
        return Failure is { } failure
            ? throw new ReplyGenerationException(failure, "Falla simulada")
            : Task.FromResult($"Respuesta a: {history[^1].Text}");
    }
}

public sealed record SentMessage(string Phone, string Text);

public sealed class FakeWhatsAppSender : IWhatsAppSender
{
    private readonly Channel<SentMessage> _sent = Channel.CreateUnbounded<SentMessage>();

    public bool IsConfigured => true;

    public string From => "whatsapp:+14155238886";

    public bool HasUnread => _sent.Reader.TryPeek(out _);

    public Task<SendResult> SendAsync(string phone, string text, CancellationToken cancellationToken)
    {
        _sent.Writer.TryWrite(new(phone, text));
        return Task.FromResult(SendResult.Ok());
    }

    /// <summary>Devuelve el siguiente mensaje enviado por WhatsApp, esperándolo si hace falta.</summary>
    public async Task<SentMessage> NextAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return await _sent.Reader.ReadAsync(timeout.Token);
    }
}
