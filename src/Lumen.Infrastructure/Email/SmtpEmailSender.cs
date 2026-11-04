using System.Net.Mail;
using Lumen.Application.Abstractions;
using Lumen.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Email;

/// <summary>Sends the quiz completion mail through SMTP (Mailpit locally).</summary>
internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly IUserRepository _users;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<SmtpOptions> options, IUserRepository users, ILogger<SmtpEmailSender> log)
    {
        _options = options.Value;
        _users = users;
        _log = log;
    }

    public async Task SendCompletionAsync(int userId, int quizId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            _log.LogWarning("Cannot send completion mail: user {UserId} not found", userId);
            return;
        }

#pragma warning disable SYSLIB0014 // SmtpClient is what the original service uses (System.Net.Mail.SmtpException in the logs)
        using var client = new SmtpClient(_options.Host, _options.Port) { Timeout = _options.TimeoutMs };
#pragma warning restore SYSLIB0014
        using var message = new MailMessage(_options.From, user.Email)
        {
            Subject = "You completed a quiz",
            Body = $"Hi {user.DisplayName}, you completed quiz #{quizId}. Check the leaderboard to see your points!"
        };
        await client.SendMailAsync(message, ct);
    }
}
