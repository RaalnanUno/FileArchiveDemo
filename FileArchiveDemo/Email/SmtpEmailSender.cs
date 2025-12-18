using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FileArchiveDemo.Email;

public sealed class SmtpEmailSender
{
    private readonly EmailOptions _opt;

    public SmtpEmailSender(EmailOptions opt) => _opt = opt;

    public async Task SendAsync(IEnumerable<string> toEmails, string subject, string bodyText, IEnumerable<(string FileName, byte[] Bytes)>? attachments = null)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromAddress));

        foreach (var e in toEmails.Where(x => !string.IsNullOrWhiteSpace(x)))
            msg.To.Add(MailboxAddress.Parse(e));

        msg.Subject = subject;

        var builder = new BodyBuilder
        {
            TextBody = bodyText
        };

        if (attachments != null)
        {
            foreach (var a in attachments)
                builder.Attachments.Add(a.FileName, a.Bytes);
        }

        msg.Body = builder.ToMessageBody();

using var client = new SmtpClient();

// Map config string -> MailKit enum
var socket = (_opt.SecureSocket ?? "None").Trim().ToLowerInvariant() switch
{
    "none" => SecureSocketOptions.None,
    "starttls" => SecureSocketOptions.StartTls,
    "sslonconnect" => SecureSocketOptions.SslOnConnect,
    _ => throw new InvalidOperationException(
        $"Invalid Email:SecureSocket value '{_opt.SecureSocket}'. Use None, StartTls, or SslOnConnect.")
};

await client.ConnectAsync(_opt.SmtpHost, _opt.SmtpPort, socket);

if (_opt.RequireAuth)
{
    if (string.IsNullOrWhiteSpace(_opt.Username))
        throw new InvalidOperationException("Email:RequireAuth is true but Email:Username is empty.");

    await client.AuthenticateAsync(_opt.Username, _opt.Password);
}
var envelopeFrom = MailboxAddress.Parse(
    string.IsNullOrWhiteSpace(_opt.EnvelopeFrom) ? _opt.FromAddress : _opt.EnvelopeFrom);

await client.SendAsync(msg, envelopeFrom, msg.To.Mailboxes);

await client.DisconnectAsync(true);

    }
}

public sealed class EmailOptions
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 25;

    // Choose exactly how TLS is handled:
    // "None"        = plain SMTP (no TLS)
    // "StartTls"    = STARTTLS if available/desired (typical on 587, sometimes 25)
    // "SslOnConnect"= implicit TLS (typical on 465)
    public string SecureSocket { get; set; } = "None";

    // Explicitly control auth (some relays reject AUTH, some require it)
    public bool RequireAuth { get; set; } = false;

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "FileArchiveDemo";
    public string EnvelopeFrom { get; set; } = "";

}
