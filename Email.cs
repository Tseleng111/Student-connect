using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com"; // or your SMTP server
    private readonly int _smtpPort = 587;
    private readonly string _adminEmail = "studentconnecta@gmail.com"; // admin email
    private readonly string _adminPassword = "xsrnpeiysoncpnej"; // app password recommended

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_adminEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_adminEmail, _adminPassword);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
