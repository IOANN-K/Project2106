using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PROJECT2106.Options;

namespace PROJECT2106.Services;

public sealed class SmtpPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpPasswordResetEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string recipientEmail, string resetUrl)
    {
        ValidateConfiguration();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = "Reset your Atlas password",
            Body =
                "We received a request to reset your Atlas password.\n\n" +
                $"Open this link to choose a new password:\n{resetUrl}\n\n" +
                "If you did not request this, you can ignore this email.",
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Timeout = 15_000
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(
                _options.Username,
                _options.Password);
        }

        await client.SendMailAsync(message);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("SMTP host is not configured.");

        if (_options.Port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP port is invalid.");

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException("SMTP sender address is not configured.");

        if (string.IsNullOrWhiteSpace(_options.Username) !=
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "SMTP username and password must be configured together.");
        }
    }
}
