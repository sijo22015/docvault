using DocVault.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocVault.Infrastructure.Services;

public class StubEmailSender : IEmailSender
{
    private readonly ILogger<StubEmailSender> _logger;
    public StubEmailSender(ILogger<StubEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL STUB] To: {To} | Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
