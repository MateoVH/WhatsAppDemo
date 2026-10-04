using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Twilio.Security;

namespace WhatsAppDemo.WhatsApp;

/// <summary>
/// Verifica la cabecera X-Twilio-Signature: un HMAC-SHA1 de la URL y los parámetros firmado con el Auth Token.
/// Así nadie más puede inyectar mensajes falsos en el webhook público.
/// </summary>
public sealed class TwilioSignatureValidator(IOptions<TwilioOptions> options)
{
    public bool IsEnabled => !string.IsNullOrWhiteSpace(options.Value.AuthToken);

    public bool IsValid(HttpRequest request, IFormCollection form)
    {
        if (!IsEnabled)
        {
            return true;
        }

        var signature = request.Headers["X-Twilio-Signature"].ToString();
        if (signature.Length == 0)
        {
            return false;
        }

        // Tras UseForwardedHeaders, el esquema y el host son los públicos del túnel: los mismos que firmó Twilio.
        var url = string.IsNullOrWhiteSpace(options.Value.WebhookUrl) ? request.GetEncodedUrl() : options.Value.WebhookUrl;
        var parameters = form.ToDictionary(field => field.Key, field => field.Value.ToString());
        return new RequestValidator(options.Value.AuthToken).Validate(url, parameters, signature);
    }
}
