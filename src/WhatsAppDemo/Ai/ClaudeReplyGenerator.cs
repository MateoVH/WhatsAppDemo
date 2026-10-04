using System.Globalization;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Options;
using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Ai;

/// <summary>Respuestas con Claude usando el SDK oficial de Anthropic para .NET.</summary>
public sealed class ClaudeReplyGenerator : IReplyGenerator
{
    // Twilio acepta hasta 1600 caracteres por mensaje.
    private const int MaxReplyLength = 1600;

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CO");

    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<ClaudeReplyGenerator> _logger;
    private readonly string _systemPrompt;
    private readonly TimeZoneInfo _timeZone;

    public ClaudeReplyGenerator(
        AnthropicClient client,
        IOptions<AnthropicOptions> options,
        IHostEnvironment environment,
        TimeProvider clock,
        ILogger<ClaudeReplyGenerator> logger)
    {
        _client = client;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
        _systemPrompt = File.ReadAllText(Path.Combine(environment.ContentRootPath, _options.SystemPromptFile));
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
    }

    public string Name => $"Claude · {_options.Model}";

    public bool IsLive => true;

    public async Task<string> GenerateReplyAsync(IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken)
    {
        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = BuildSystemPrompt(),
            Messages = [.. history.Select(turn => new BetaMessageParam
            {
                Role = turn.FromCustomer ? Role.User : Role.Assistant,
                Content = turn.Text,
            })],
            OutputConfig = new BetaOutputConfig { Effort = _options.Effort },
            // Si el modelo declina por políticas de seguridad, la API reintenta con su modelo de respaldo por defecto.
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Default(),
        };

        BetaMessage response;
        try
        {
            response = await _client.Beta.Messages.Create(parameters, cancellationToken);
        }
        catch (AnthropicUnauthorizedException ex)
        {
            throw new ReplyGenerationException(ReplyFailure.InvalidApiKey, "Anthropic rejected the API key.", ex);
        }
        catch (AnthropicNotFoundException ex)
        {
            throw new ReplyGenerationException(ReplyFailure.ModelNotFound, $"Model '{_options.Model}' was not found.", ex);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw new ReplyGenerationException(ReplyFailure.RateLimited, "Anthropic API rate limit reached.", ex);
        }
        catch (Anthropic5xxException ex)
        {
            throw new ReplyGenerationException(ReplyFailure.Unavailable, "Anthropic API is unavailable.", ex);
        }
        catch (AnthropicIOException ex)
        {
            throw new ReplyGenerationException(ReplyFailure.Network, "Could not reach the Anthropic API.", ex);
        }
        catch (AnthropicApiException ex)
        {
            throw new ReplyGenerationException(ReplyFailure.ApiError, $"Anthropic API error: {ex.Message}", ex);
        }

        // Si ni el modelo de respaldo quiso responder, mejor que lo revise una persona.
        if (response.StopReason == "refusal")
        {
            throw new ReplyGenerationException(ReplyFailure.Refused, "The model declined to answer.");
        }

        var reply = string.Concat(response.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(block => block.Text)).Trim();
        if (reply.Length == 0)
        {
            throw new ReplyGenerationException(ReplyFailure.EmptyReply, $"The model returned no text (stop_reason: {response.StopReason}).");
        }

        if (response.StopReason == "max_tokens")
        {
            _logger.LogWarning("La respuesta llegó al tope de {MaxTokens} tokens y quedó cortada", _options.MaxTokens);
        }

        return reply.Length <= MaxReplyLength ? reply : string.Concat(reply.AsSpan(0, MaxReplyLength - 1), "…");
    }

    private string BuildSystemPrompt()
    {
        var now = TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), _timeZone);
        return $"{_systemPrompt}\n\nFecha y hora local del negocio: {now.ToString("dddd d 'de' MMMM 'de' yyyy, h:mm tt", Spanish)}.";
    }
}
