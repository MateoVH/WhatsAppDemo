using Microsoft.AspNetCore.SignalR;

namespace WhatsAppDemo.Inbox;

/// <summary>Eventos que el servidor empuja a la bandeja web.</summary>
public interface IInboxClient
{
    Task MessageAdded(InboxMessage message);
    Task MessageUpdated(InboxMessage message);
    Task ConversationUpdated(ConversationSummary conversation);
    Task TypingChanged(string conversationId, bool isTyping);
}

/// <summary>
/// Hub de solo envío: el servidor notifica cada cambio en tiempo real
/// y las acciones del asesor viajan por la API REST.
/// </summary>
public sealed class InboxHub : Hub<IInboxClient>;
