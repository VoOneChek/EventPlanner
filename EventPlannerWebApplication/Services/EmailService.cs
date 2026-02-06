using System.Net;
using System.Net.Mail;

public class EmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var host = Environment.GetEnvironmentVariable("SmtpClientHost");
        var stringPort = Environment.GetEnvironmentVariable("SmtpClientPort");
        var login = Environment.GetEnvironmentVariable("MailLogin");
        var password = Environment.GetEnvironmentVariable("MailPassword");

        if (host == null || stringPort == null || login == null || password == null)
        {
            throw new Exception("ENV SmtpClientHost = null");
        }
        var port = int.Parse(stringPort);

        var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(login, password),
            EnableSsl = true
        };

        var message = new MailMessage(login!, to, subject, body);

        await smtp.SendMailAsync(message);
    }
}
