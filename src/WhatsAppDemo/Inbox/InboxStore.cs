namespace WhatsAppDemo.Inbox;

/// <summary>
/// Bandeja en memoria. Para una demo basta: se pierde al reiniciar la app.
/// Todas las operaciones toman el mismo lock y devuelven copias inmutables.
/// </summary>
public sealed class InboxStore(TimeProvider clock)
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Conversation> _conversations = [];
    private readonly HashSet<string> _seenExternalIds = [];
    private long _lastSeq;

    public IReadOnlyList<ConversationSummary> GetConversations()
    {
        lock (_gate)
        {
            return [.. _conversations.Values.Select(c => c.ToSummary()).OrderByDescending(c => c.UpdatedAt)];
        }
    }

    public ConversationDetail? GetConversation(string conversationId)
    {
        lock (_gate)
        {
            return _conversations.TryGetValue(conversationId, out var c) ? new(c.ToSummary(), [.. c.Messages]) : null;
        }
    }

    public bool IsAutoReplyEnabled(string conversationId)
    {
        lock (_gate)
        {
            return _conversations.TryGetValue(conversationId, out var c) && c.AutoReply;
        }
    }

    /// <summary>Guarda un mensaje del cliente. Devuelve null si ya se había recibido (reintento del webhook).</summary>
    public InboxUpdate? AddIncoming(IncomingMessage incoming)
    {
        lock (_gate)
        {
            if (incoming.ExternalId is { Length: > 0 } externalId && !_seenExternalIds.Add(externalId))
            {
                return null;
            }

            var digits = new string([.. incoming.Phone.Where(char.IsAsciiDigit)]);
            var id = incoming.IsSimulated ? $"sim-{digits}" : digits;
            if (!_conversations.TryGetValue(id, out var conversation))
            {
                conversation = new Conversation(id, $"+{digits}", incoming.IsSimulated);
                _conversations.Add(id, conversation);
            }

            if (!string.IsNullOrWhiteSpace(incoming.Name))
            {
                conversation.Name = incoming.Name.Trim();
            }

            return Append(conversation, MessageAuthor.Customer, incoming.Text, MessageStatus.Received);
        }
    }

    /// <summary>Guarda un mensaje saliente (IA o asesor) en estado pendiente de envío.</summary>
    public InboxUpdate? AddOutgoing(string conversationId, MessageAuthor author, string text, long? replyToSeq = null)
    {
        lock (_gate)
        {
            return _conversations.TryGetValue(conversationId, out var c)
                ? Append(c, author, text, MessageStatus.Pending, replyToSeq)
                : null;
        }
    }

    /// <summary>Guarda una nota interna: se ve en la bandeja, pero no se envía ni la lee la IA.</summary>
    public InboxUpdate? AddNote(string conversationId, NoteKind note, string? error = null)
    {
        // El texto es solo un respaldo en inglés: la bandeja traduce la nota a partir de su tipo.
        var text = note switch
        {
            NoteKind.AutoReplyOn => "AI resumed",
            NoteKind.AutoReplyOff => "AI paused: an agent took over",
            NoteKind.AiFailed => $"AI could not reply ({error})",
            _ => note.ToString(),
        };

        lock (_gate)
        {
            return _conversations.TryGetValue(conversationId, out var c)
                ? Append(c, MessageAuthor.System, text, MessageStatus.Received, note: note, error: error)
                : null;
        }
    }

    public InboxMessage? UpdateStatus(string conversationId, string messageId, MessageStatus status, string? error = null)
    {
        lock (_gate)
        {
            if (!_conversations.TryGetValue(conversationId, out var c))
            {
                return null;
            }

            var index = c.Messages.FindIndex(m => m.Id == messageId);
            return index < 0 ? null : c.Messages[index] = c.Messages[index] with { Status = status, Error = error };
        }
    }

    public AutoReplyChange? SetAutoReply(string conversationId, bool enabled)
    {
        lock (_gate)
        {
            if (!_conversations.TryGetValue(conversationId, out var c))
            {
                return null;
            }

            var changed = c.AutoReply != enabled;
            c.AutoReply = enabled;
            return new(c.ToSummary(), changed);
        }
    }

    /// <summary>
    /// Devuelve lo que la IA debe responder, o null si no hay nada pendiente (o la IA está en pausa).
    /// </summary>
    public PendingReply? GetPendingReply(string conversationId, int historyLimit)
    {
        lock (_gate)
        {
            if (!_conversations.TryGetValue(conversationId, out var c) || !c.AutoReply)
            {
                return null;
            }

            // Orden lógico para el modelo: cada respuesta de la IA va justo después del mensaje que respondió,
            // aunque se haya guardado más tarde (el cliente pudo escribir de nuevo mientras la IA generaba).
            var ordered = c.Messages
                .Where(m => m.Author != MessageAuthor.System)
                .OrderBy(m => m.ReplyToSeq is { } replyTo ? replyTo + 0.5 : m.Seq)
                .ToList();

            if (ordered.Count == 0 || ordered[^1].Author != MessageAuthor.Customer)
            {
                return null;
            }

            List<ChatTurn> history =
            [
                .. ordered
                    .TakeLast(historyLimit)
                    .SkipWhile(m => m.Author != MessageAuthor.Customer)
                    .Select(m => new ChatTurn(m.Author == MessageAuthor.Customer, m.Text)),
            ];
            return new(history, ordered[^1].Seq);
        }
    }

    private InboxUpdate Append(
        Conversation c, MessageAuthor author, string text, MessageStatus status,
        long? replyToSeq = null, NoteKind? note = null, string? error = null)
    {
        var now = clock.GetUtcNow();
        var message = new InboxMessage(
            Guid.CreateVersion7(now).ToString("N"), c.Id, ++_lastSeq, author, text, now, status, error, replyToSeq, note);
        c.Messages.Add(message);

        // Las notas internas no reordenan la lista de conversaciones.
        if (author != MessageAuthor.System)
        {
            c.UpdatedAt = now;
        }

        return new(message, c.ToSummary());
    }

    private sealed class Conversation(string id, string phone, bool isSimulated)
    {
        public string Id { get; } = id;
        public string Phone { get; } = phone;
        public bool IsSimulated { get; } = isSimulated;
        public string Name { get; set; } = phone;
        public bool AutoReply { get; set; } = true;
        public DateTimeOffset UpdatedAt { get; set; }
        public List<InboxMessage> Messages { get; } = [];

        public ConversationSummary ToSummary() => new(
            Id, Phone, Name, AutoReply, IsSimulated, Messages.LastOrDefault(m => m.Author != MessageAuthor.System), UpdatedAt);
    }
}
