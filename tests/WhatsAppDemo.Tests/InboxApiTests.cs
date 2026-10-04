using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppDemo.Ai;
using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Tests;

public class InboxApiTests
{
    [Fact]
    public async Task Agent_reply_pauses_the_ai_and_goes_out_by_whatsapp()
    {
        await using var app = new InboxApp();
        var client = app.CreateClient();
        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Quiero hablar con una persona"));
        await app.WhatsApp.NextAsync();

        var response = await client.PostAsJsonAsync("/api/conversations/573001112233/messages", new { text = "Hola Ana, soy Laura 👋" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new SentMessage("+573001112233", "Hola Ana, soy Laura 👋"), await app.WhatsApp.NextAsync());

        // Con la IA en pausa, el siguiente mensaje del cliente queda para el agente.
        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "¡Gracias!"));
        var conversation = await app.WaitForConversationAsync("573001112233", c => c.Messages[^1].Text == "¡Gracias!");
        await Task.Delay(300);

        Assert.False(conversation.Conversation.AutoReply);
        Assert.Contains(conversation.Messages, m => m.Author == MessageAuthor.System && m.Note == NoteKind.AutoReplyOff);
        Assert.Single(app.Ai.Requests);
        Assert.False(app.WhatsApp.HasUnread);
    }

    [Fact]
    public async Task Reenabling_the_ai_answers_what_the_customer_wrote_meanwhile()
    {
        await using var app = new InboxApp();
        var client = app.CreateClient();
        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola"));
        await app.WhatsApp.NextAsync();
        await client.PutAsJsonAsync("/api/conversations/573001112233/auto-reply", new { enabled = false });
        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "¿Siguen abiertos?"));

        await client.PutAsJsonAsync("/api/conversations/573001112233/auto-reply", new { enabled = true });

        Assert.Equal("Respuesta a: ¿Siguen abiertos?", (await app.WhatsApp.NextAsync()).Text);
    }

    [Fact]
    public async Task Ai_failure_leaves_a_translatable_note_for_the_agent()
    {
        await using var app = new InboxApp();
        app.Ai.Failure = ReplyFailure.InvalidApiKey;

        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola"));

        var conversation = await app.WaitForConversationAsync("573001112233", c => c.Messages[^1].Author == MessageAuthor.System);
        Assert.Equal((NoteKind.AiFailed, "invalidApiKey"), (conversation.Messages[^1].Note, conversation.Messages[^1].Error));
        Assert.False(app.WhatsApp.HasUnread);
    }

    [Fact]
    public async Task Simulated_conversations_never_go_out_by_whatsapp()
    {
        await using var app = new InboxApp();

        var response = await app.CreateClient().PostAsJsonAsync("/api/simulator/messages",
            new { phone = "+57 300 555 0101", name = "Cliente demo", text = "¿A qué hora abren?" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var conversation = await app.WaitForConversationAsync("sim-573005550101", c => c.Messages[^1].Status == MessageStatus.Simulated);
        Assert.True(conversation.Conversation.IsSimulated);
        Assert.Equal("Respuesta a: ¿A qué hora abren?", conversation.Messages[^1].Text);
        Assert.False(app.WhatsApp.HasUnread);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_agent_reply_is_rejected(string text)
    {
        await using var app = new InboxApp();
        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola"));

        var response = await app.CreateClient().PostAsJsonAsync("/api/conversations/573001112233/messages", new { text });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Requests_arriving_through_a_tunnel_can_only_reach_the_webhook()
    {
        await using var app = new InboxApp();
        var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/conversations");
        request.Headers.Add("X-Forwarded-For", "203.0.113.10");

        var inbox = await client.SendAsync(request);
        var webhook = await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola"), forwardedFor: "203.0.113.10");

        Assert.Equal(HttpStatusCode.Forbidden, inbox.StatusCode);
        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
    }

    [Fact]
    public async Task Inbox_hub_pushes_new_messages_in_real_time()
    {
        await using var app = new InboxApp();
        var server = app.Server;
        var aiReply = new TaskCompletionSource<InboxMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typing = new List<bool>();
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/hubs/inbox"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)))
            .Build();
        connection.On<string, bool>(nameof(IInboxClient.TypingChanged), (_, isTyping) => typing.Add(isTyping));
        connection.On<InboxMessage>(nameof(IInboxClient.MessageAdded), message =>
        {
            if (message.Author == MessageAuthor.Ai)
            {
                aiReply.TrySetResult(message);
            }
        });
        await connection.StartAsync();

        await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola"));

        var reply = await aiReply.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Respuesta a: Hola", reply.Text);
        Assert.Equal(new[] { true, false }, typing);
    }
}
