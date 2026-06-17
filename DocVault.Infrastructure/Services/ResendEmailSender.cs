using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocVault.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocVault.Infrastructure.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient http, IConfiguration config, ILogger<ResendEmailSender> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var apiKey = _config["Email:ResendApiKey"];
        var from   = _config["Email:FromAddress"] ?? "onboarding@resend.dev";
        var fromName = _config["Email:FromName"] ?? "DocVault";

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Email service is not configured on the server. Please contact the administrator.");

        var payload = new
        {
            from    = $"{fromName} <{from}>",
            to      = new[] { to },
            subject = subject,
            html    = body
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct);
                _logger.LogError("[EMAIL] Resend error {Status}: {Body}", res.StatusCode, err);
                throw new InvalidOperationException("Failed to send verification email. Please try again.");
            }
            _logger.LogInformation("[EMAIL] Sent via Resend to {To}", to);
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Unexpected failure sending to {To}", to);
            throw new InvalidOperationException("Failed to send email. Please try again later.");
        }
    }
}
