using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using WhatsAppDemo.Ai;
using WhatsAppDemo.Inbox;
using WhatsAppDemo.WhatsApp;

var builder = WebApplication.CreateBuilder(args);

// Enums legibles ("customer", "pending"...) tanto en la API REST como en SignalR.
var enumsAsStrings = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(enumsAsStrings));
builder.Services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(enumsAsStrings));
builder.Services.AddValidation();

builder.Services.AddOptions<InboxOptions>().BindConfiguration(InboxOptions.Section);
builder.Services.AddOptions<TwilioOptions>().BindConfiguration(TwilioOptions.Section);
builder.Services.AddOptions<AnthropicOptions>().BindConfiguration(AnthropicOptions.Section);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<InboxStore>();
builder.Services.AddSingleton<InboxService>();
builder.Services.AddSingleton<AutoReplyQueue>();
builder.Services.AddHostedService<AutoReplyWorker>();
builder.Services.AddSingleton<IWhatsAppSender, TwilioWhatsAppSender>();
builder.Services.AddSingleton<TwilioSignatureValidator>();

// Con API key responde Claude; sin ella, respuestas simuladas para poder probar la bandeja igual.
var anthropicKey = new[] { builder.Configuration["Anthropic:ApiKey"], builder.Configuration["ANTHROPIC_API_KEY"] }
    .FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));
if (anthropicKey is null)
{
    builder.Services.AddSingleton<IReplyGenerator, DemoReplyGenerator>();
}
else
{
    builder.Services.AddSingleton(new AnthropicClient { ApiKey = anthropicKey, Timeout = TimeSpan.FromSeconds(60) });
    builder.Services.AddSingleton<IReplyGenerator, ClaudeReplyGenerator>();
}

var app = builder.Build();

// Detrás de un túnel, la URL pública llega en X-Forwarded-*. Solo se confía en proxies locales
// (el valor por defecto), que es justo donde corre ngrok o dev tunnels.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });
if (app.Services.GetRequiredService<IOptions<InboxOptions>>().Value.LocalOnly)
{
    app.UseLocalOnlyInbox();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapInboxApi();
app.MapTwilioWebhook();
app.MapHub<InboxHub>("/hubs/inbox");

var ai = app.Services.GetRequiredService<IReplyGenerator>();
var whatsApp = app.Services.GetRequiredService<IWhatsAppSender>();
app.Logger.LogInformation("IA: {Ai}", ai.IsLive ? ai.Name : "modo demo, configura Anthropic:ApiKey para usar Claude");
app.Logger.LogInformation("WhatsApp: {WhatsApp}", whatsApp.IsConfigured
    ? $"Twilio ({whatsApp.From}), webhook en {TwilioWebhook.Path}"
    : "modo simulado, configura Twilio:AccountSid y Twilio:AuthToken");

app.Run();
