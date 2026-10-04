using Microsoft.AspNetCore.SignalR;
using WhatsAppDemo.Ai;
using WhatsAppDemo.WhatsApp;

namespace WhatsAppDemo.Inbox;

/// <summary>
/// Casos de uso de la bandeja: recibir, responder y pausar la IA.
/// Cada cambio se guarda en el store y se notifica por SignalR a todas las bandejas abiertas.
/// </summary>
public sealed class InboxService(
    InboxStore store,
    IHubContext<InboxHub, IInboxClient> hub,
    IWhatsAppSender whatsApp,
    AutoReplyQueue autoReplies,
    ILogger<InboxService> logger)
{
    public async Task ReceiveAsync(IncomingMessage incoming)
    {
        if (store.AddIncoming(incoming) is not { } update)
        {
            logger.LogInformation("Mensaje {ExternalId} duplicado: se ignora", incoming.ExternalId);
            return;
        }

        await PublishAsync(update);
        if (update.Conversation.AutoReply)
        {
            autoReplies.Enqueue(update.Conversation.Id);
        }
    }

    /// <summary>Respuesta escrita por un asesor: pausa la IA para que no le pise la conversación.</summary>
    public async Task<InboxMessage?> SendAgentReplyAsync(string conversationId, string text, CancellationToken cancellationToken)
    {
        return await SetAutoReplyAsync(conversationId, enabled: false) is null
            ? null
            : await SendAsync(conversationId, text, MessageAuthor.Agent, cancellationToken: cancellationToken);
    }

    public async Task<InboxMessage?> SendAsync(
        string conversationId, string text, MessageAuthor author, long? replyToSeq = null, CancellationToken cancellationToken = default)
    {
        if (store.AddOutgoing(conversationId, author, text, replyToSeq) is not { } update)
        {
            return null;
        }

        await PublishAsync(update);

        var (status, error) = await DeliverAsync(update.Conversation, text, cancellationToken);
        var delivered = store.UpdateStatus(conversationId, update.Message.Id, status, error) ?? update.Message;
        await hub.Clients.All.MessageUpdated(delivered);
        return delivered;
    }

    public async Task<ConversationSummary?> SetAutoReplyAsync(string conversationId, bool enabled)
    {
        if (store.SetAutoReply(conversationId, enabled) is not { } change)
        {
            return null;
        }

        if (change.Changed)
        {
            await AddNoteAsync(conversationId, enabled ? NoteKind.AutoReplyOn : NoteKind.AutoReplyOff);
        }

        await hub.Clients.All.ConversationUpdated(change.Conversation);

        // Al reactivarla, la IA responde lo que el cliente haya escrito mientras estaba en pausa.
        if (enabled)
        {
            autoReplies.Enqueue(conversationId);
        }

        return change.Conversation;
    }

    public async Task AddNoteAsync(string conversationId, NoteKind note, string? error = null)
    {
        if (store.AddNote(conversationId, note, error) is { } update)
        {
            await hub.Clients.All.MessageAdded(update.Message);
        }
    }

    public Task SetTypingAsync(string conversationId, bool isTyping) =>
        hub.Clients.All.TypingChanged(conversationId, isTyping);

    private async Task<(MessageStatus Status, string? Error)> DeliverAsync(
        ConversationSummary conversation, string text, CancellationToken cancellationToken)
    {
        // Las conversaciones del simulador (o sin credenciales de Twilio) solo viven en la bandeja.
        if (conversation.IsSimulated || !whatsApp.IsConfigured)
        {
            return (MessageStatus.Simulated, null);
        }

        var result = await whatsApp.SendAsync(conversation.Phone, text, cancellationToken);
        return result.Success ? (MessageStatus.Sent, null) : (MessageStatus.Failed, result.Error);
    }

    private async Task PublishAsync(InboxUpdate update)
    {
        await hub.Clients.All.MessageAdded(update.Message);
        await hub.Clients.All.ConversationUpdated(update.Conversation);
    }
}
