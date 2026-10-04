using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppDemo.Ai;
using WhatsAppDemo.Inbox;

namespace WhatsAppDemo.Tests;

/// <summary>Verifica la petición exacta que se envía a Claude, interceptando el HTTP del SDK.</summary>
public sealed class ClaudeReplyGeneratorTests : IDisposable
{
    private readonly FakeAnthropicApi _api = new();
    private readonly string _contentRoot = Directory.CreateTempSubdirectory().FullName;

    public ClaudeReplyGeneratorTests() =>
        File.WriteAllText(Path.Combine(_contentRoot, "prompt.md"), "Eres un asistente de prueba.");

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    [Fact]
    public async Task Sends_the_conversation_to_claude_and_returns_its_text()
    {
        _api.Respond(HttpStatusCode.OK, Message("end_turn", "¡Abrimos a las 7:00 a. m.! ☕"));

        var reply = await CreateGenerator().GenerateReplyAsync(
            [new(true, "Hola"), new(false, "¡Hola! ¿En qué te ayudo?"), new(true, "¿A qué hora abren?")],
            CancellationToken.None);

        Assert.Equal("¡Abrimos a las 7:00 a. m.! ☕", reply);
        var request = JsonDocument.Parse(_api.RequestBody!).RootElement;
        Assert.Equal("claude-opus-5-5", request.GetProperty("model").GetString());
        Assert.Equal("low", request.GetProperty("output_config").GetProperty("effort").GetString());
        Assert.Equal("default", request.GetProperty("fallbacks").GetString());
        Assert.StartsWith("Eres un asistente de prueba.", request.GetProperty("system").GetString());
        Assert.Equal(
            new[] { "user", "assistant", "user" },
            request.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("role").GetString()));
        Assert.Contains("server-side-fallback-2026-07-01", _api.BetaHeader);
    }

    [Fact]
    public async Task Refusal_is_left_for_a_human_to_answer()
    {
        _api.Respond(HttpStatusCode.OK, Message("refusal", text: null));

        var error = await Assert.ThrowsAsync<ReplyGenerationException>(
            () => CreateGenerator().GenerateReplyAsync([new(true, "...")], CancellationToken.None));

        Assert.Equal(ReplyFailure.Refused, error.Reason);
    }

    [Fact]
    public async Task Invalid_api_key_is_reported_in_plain_words()
    {
        _api.Respond(HttpStatusCode.Unauthorized,
            """{"type":"error","error":{"type":"authentication_error","message":"invalid x-api-key"}}""");

        var error = await Assert.ThrowsAsync<ReplyGenerationException>(
            () => CreateGenerator().GenerateReplyAsync([new(true, "Hola")], CancellationToken.None));

        Assert.Equal(ReplyFailure.InvalidApiKey, error.Reason);
    }

    private ClaudeReplyGenerator CreateGenerator() => new(
        new AnthropicClient { ApiKey = "sk-ant-test", HttpClient = new HttpClient(_api), MaxRetries = 0 },
        Options.Create(new AnthropicOptions { SystemPromptFile = "prompt.md" }),
        new TestEnvironment(_contentRoot),
        TimeProvider.System,
        NullLogger<ClaudeReplyGenerator>.Instance);

    private static string Message(string stopReason, string? text) => JsonSerializer.Serialize(new
    {
        id = "msg_test",
        type = "message",
        role = "assistant",
        model = "claude-opus-5-5",
        content = text is null ? [] : new object[] { new { type = "text", text } },
        stop_reason = stopReason,
        stop_sequence = (string?)null,
        usage = new { input_tokens = 42, output_tokens = 12 },
    });

    private sealed class FakeAnthropicApi : HttpMessageHandler
    {
        private HttpStatusCode _status;
        private string _response = "";

        public string? RequestBody { get; private set; }

        public string BetaHeader { get; private set; } = "";

        public void Respond(HttpStatusCode status, string json) => (_status, _response) = (status, json);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            BetaHeader = request.Headers.TryGetValues("anthropic-beta", out var betas) ? string.Join(",", betas) : "";
            return new HttpResponseMessage(_status) { Content = new StringContent(_response, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class TestEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "WhatsAppDemo";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
