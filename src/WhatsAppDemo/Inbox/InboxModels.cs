namespace WhatsAppDemo.Inbox;

public enum MessageAuthor { Customer, Ai, Agent, System }

public enum MessageStatus { Received, Pending, Sent, Simulated, Failed }

/// <summary>Tipo de nota interna. La bandeja la muestra traducida al idioma de quien la mira.</summary>
public enum NoteKind { AutoReplyOn, AutoReplyOff, AiFailed }

/// <summary>Un mensaje de la bandeja. Es inmutable: un cambio de estado produce una copia nueva.</summary>
/// <param name="Error">Detalle de la falla: el error de Twilio o, en notas <see cref="NoteKind.AiFailed"/>, el código del motivo.</param>
/// <param name="ReplyToSeq">En respuestas de la IA, el último mensaje del cliente que tuvo en cuenta.</param>
public sealed record InboxMessage(
    string Id,
    string ConversationId,
    long Seq,
    MessageAuthor Author,
    string Text,
    DateTimeOffset CreatedAt,
    MessageStatus Status,
    string? Error = null,
    long? ReplyToSeq = null,
    NoteKind? Note = null);

public sealed record ConversationSummary(
    string Id,
    string Phone,
    string Name,
    bool AutoReply,
    bool IsSimulated,
    InboxMessage? LastMessage,
    DateTimeOffset UpdatedAt);

public sealed record ConversationDetail(ConversationSummary Conversation, IReadOnlyList<InboxMessage> Messages);

/// <summary>Resultado de guardar un mensaje: el mensaje y cómo quedó su conversación.</summary>
public sealed record InboxUpdate(InboxMessage Message, ConversationSummary Conversation);

public sealed record AutoReplyChange(ConversationSummary Conversation, bool Changed);

/// <summary>Mensaje entrante normalizado, venga de Twilio o del simulador.</summary>
public sealed record IncomingMessage(string Phone, string Name, string Text, string? ExternalId, bool IsSimulated);

/// <summary>Un turno de la conversación tal como lo ve el modelo de IA.</summary>
public sealed record ChatTurn(bool FromCustomer, string Text);

/// <summary>Historial que la IA debe responder: siempre empieza y termina con un mensaje del cliente.</summary>
public sealed record PendingReply(IReadOnlyList<ChatTurn> History, long ReplyToSeq);
