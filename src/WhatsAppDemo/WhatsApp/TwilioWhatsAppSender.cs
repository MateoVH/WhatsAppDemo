using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace WhatsAppDemo.WhatsApp;

/// <summary>Envía mensajes de WhatsApp con la API REST de Twilio.</summary>
public sealed class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly TwilioOptions _options;
    private readonly TwilioRestClient? _client;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public TwilioWhatsAppSender(IOptions<TwilioOptions> options, ILogger<TwilioWhatsAppSender> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (_options.IsConfigured)
        {
            _client = new TwilioRestClient(_options.AccountSid, _options.AuthToken);
        }
    }

    public bool IsConfigured => _client is not null;

    public string From => _options.WhatsAppFrom;

    public async Task<SendResult> SendAsync(string phone, string text, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return SendResult.Failed("Twilio is not configured.");
        }

        try
        {
            var message = await MessageResource
                .CreateAsync(new CreateMessageOptions(new PhoneNumber($"whatsapp:{phone}"))
                {
                    From = new PhoneNumber(_options.WhatsAppFrom),
                    Body = text,
                }, _client)
                .WaitAsync(cancellationToken);

            _logger.LogInformation("Mensaje {Sid} enviado por WhatsApp", message.Sid);
            return SendResult.Ok();
        }
        catch (ApiException ex)
        {
            // Errores típicos: 63015 (el número no se unió al sandbox) o 63016 (fuera de la ventana de 24 h).
            _logger.LogWarning(ex, "Twilio rechazó el mensaje (código {Code})", ex.Code);
            return SendResult.Failed($"Twilio {ex.Code}: {ex.Message}");
        }
    }
}
