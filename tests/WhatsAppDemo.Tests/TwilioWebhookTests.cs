using System.Net;
using System.Net.Http.Json;
using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Tests;

public class TwilioWebhookTests
{
    [Fact]
    public async Task Signed_message_reaches_the_inbox_and_the_ai_replies_by_whatsapp()
    {
        await using var app = new InboxApp();

        var response = await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola, ¿tienen domicilios?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new SentMessage("+573001112233", "Respuesta a: Hola, ¿tienen domicilios?"), await app.WhatsApp.NextAsync());

        var conversation = await app.WaitForConversationAsync("573001112233", c => c.Messages[^1].Status == MessageStatus.Sent);
        Assert.Equal("Ana", conversation.Conversation.Name);
        Assert.Collection(conversation.Messages,
            m => Assert.Equal((MessageAuthor.Customer, "Hola, ¿tienen domicilios?"), (m.Author, m.Text)),
            m => Assert.Equal((MessageAuthor.Ai, "Respuesta a: Hola, ¿tienen domicilios?"), (m.Author, m.Text)));
    }

    [Theory]
    [InlineData("firma-falsa")]
    [InlineData("")]
    public async Task Message_with_invalid_signature_is_rejected(string signature)
    {
        await using var app = new InboxApp();

        var response = await app.PostWebhookAsync(InboxApp.WhatsAppMessage("+573001112233", "Hola"), signature);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(await app.CreateClient().GetFromJsonAsync<ConversationSummary[]>("/api/conversations", InboxApp.Json) ?? []);
    }

    [Fact]
    public async Task Repeated_delivery_of_the_same_message_is_stored_once()
    {
        await using var app = new InboxApp();
        var message = InboxApp.WhatsAppMessage("+573001112233", "Hola", messageSid: "SM123");

        await app.PostWebhookAsync(message);
        await app.PostWebhookAsync(message);

        var conversation = await app.WaitForConversationAsync("573001112233", c => c.Messages.Count >= 2);
        Assert.Single(conversation.Messages, m => m.Author == MessageAuthor.Customer);
    }
}
