using System.Text.Json;
using Microsoft.Extensions.Options;
using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Ai;

/// <summary>
/// Responde con IA en segundo plano, fuera del webhook: Twilio recibe su 200 al instante
/// y la respuesta sale después por la API REST. Un único consumidor evita respuestas duplicadas.
/// </summary>
public sealed class AutoReplyWorker(
    AutoReplyQueue queue,
    InboxStore store,
    InboxService inbox,
    IReplyGenerator generator,
    IOptions<InboxOptions> options,
    ILogger<AutoReplyWorker> logger) : BackgroundService
{
    // Tope de respuestas seguidas por si el cliente no para de escribir mientras la IA genera.
    private const int MaxRepliesPerTurn = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var conversationId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ReplyAsync(conversationId, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "No se pudo responder la conversación {ConversationId}", conversationId);
            }
        }
    }

    private async Task ReplyAsync(string conversationId, CancellationToken cancellationToken)
    {
        // Se repite si llegaron mensajes nuevos mientras la IA generaba la respuesta anterior.
        for (var i = 0; i < MaxRepliesPerTurn; i++)
        {
            if (store.GetPendingReply(conversationId, options.Value.HistoryLimit) is not { } pending)
            {
                return;
            }

            string reply;
            await inbox.SetTypingAsync(conversationId, true);
            try
            {
                reply = await generator.GenerateReplyAsync(pending.History, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "La IA falló en la conversación {ConversationId}", conversationId);
                var reason = ex is ReplyGenerationException failure ? failure.Reason : ReplyFailure.Unexpected;
                await inbox.AddNoteAsync(conversationId, NoteKind.AiFailed, JsonNamingPolicy.CamelCase.ConvertName(reason.ToString()));
                return;
            }
            finally
            {
                await inbox.SetTypingAsync(conversationId, false);
            }

            // Si un asesor tomó la conversación mientras la IA pensaba, la respuesta se descarta.
            if (!store.IsAutoReplyEnabled(conversationId))
            {
                return;
            }

            await inbox.SendAsync(conversationId, reply, MessageAuthor.Ai, pending.ReplyToSeq, cancellationToken);
        }
    }
}
