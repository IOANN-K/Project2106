namespace PROJECT2106.Services;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string recipientEmail, string resetUrl);
}
