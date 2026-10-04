using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Tests;

public class InboxStoreTests
{
    private readonly InboxStore _store = new(TimeProvider.System);

    [Fact]
    public void Message_that_arrives_while_the_ai_is_generating_is_answered_next()
    {
        _store.AddIncoming(Incoming("Hola"));
        var first = _store.GetPendingReply("573001112233", historyLimit: 20)!;

        // El cliente escribe otra vez antes de que la IA termine; la respuesta se guarda después.
        _store.AddIncoming(Incoming("¿Tienen pandebono?"));
        _store.AddOutgoing("573001112233", MessageAuthor.Ai, "¡Hola! ¿En qué te ayudo?", first.ReplyToSeq);

        var second = _store.GetPendingReply("573001112233", historyLimit: 20)!;
        Assert.Equal(
            new ChatTurn[] { new(true, "Hola"), new(false, "¡Hola! ¿En qué te ayudo?"), new(true, "¿Tienen pandebono?") },
            second.History);

        _store.AddOutgoing("573001112233", MessageAuthor.Ai, "¡Sí! A $3.500", second.ReplyToSeq);
        Assert.Null(_store.GetPendingReply("573001112233", historyLimit: 20));
    }

    [Fact]
    public void History_is_trimmed_and_always_starts_with_the_customer()
    {
        _store.AddIncoming(Incoming("Uno"));
        var pending = _store.GetPendingReply("573001112233", historyLimit: 20)!;
        _store.AddOutgoing("573001112233", MessageAuthor.Ai, "Respuesta", pending.ReplyToSeq);
        _store.AddIncoming(Incoming("Dos"));

        var history = _store.GetPendingReply("573001112233", historyLimit: 2)!.History;

        Assert.Equal(new[] { new ChatTurn(true, "Dos") }, history);
    }

    [Fact]
    public void Paused_conversation_has_nothing_pending_for_the_ai()
    {
        _store.AddIncoming(Incoming("Hola"));

        _store.SetAutoReply("573001112233", enabled: false);

        Assert.Null(_store.GetPendingReply("573001112233", historyLimit: 20));
    }

    [Fact]
    public void Internal_notes_are_neither_previewed_nor_shown_to_the_ai()
    {
        _store.AddIncoming(Incoming("Hola"));

        _store.AddNote("573001112233", NoteKind.AutoReplyOff);

        Assert.Equal("Hola", _store.GetConversations().Single().LastMessage?.Text);
        Assert.Equal(new[] { new ChatTurn(true, "Hola") }, _store.GetPendingReply("573001112233", historyLimit: 20)!.History);
    }

    private static IncomingMessage Incoming(string text) =>
        new("+573001112233", "Ana", text, ExternalId: null, IsSimulated: false);
}
