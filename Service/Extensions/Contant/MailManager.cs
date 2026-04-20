using Core.Entities;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Service.Extensions.Abstract;
using System.Web;

namespace Service.Extensions.Contant
{
    public class MailManager : IMailService
    {
        private readonly IConfiguration _configuration;
        private string messageTitle = "";

        public MailManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendContactMailAsync(IncomingMessage model)
        {
            var smtpInfo = await GetSmtpInfoAsync();

            var emailMessage = new MimeMessage();

            // Tablonun degisken kismi
            string tableRows = "";

            emailMessage.From.Add(new MailboxAddress("KOZLOW E-Ticaret İletisim", smtpInfo.Mail));
            emailMessage.Subject = $"İletisim Formu: {model.Subject}";
            messageTitle = "Yeni Bir İletisim Formu Mesaji";

            // Iletisim formu
            tableRows += $@"
                    <tr><td class=""label"">Konu:</td><td>{HttpUtility.HtmlEncode(model.Subject ?? string.Empty)}</td></tr>
                    <tr><td class=""label"" style=""vertical-align: top;"">Mesaj:</td>
                        <td class=""message-content"">{(HttpUtility.HtmlEncode(model.Message) ?? string.Empty).Replace(Environment.NewLine, "<br />")}</td></tr>
                ";

            // Alici
            emailMessage.To.Add(MailboxAddress.Parse(smtpInfo.Mail));

            // Cevaplanacak kisi
            emailMessage.ReplyTo.Add(new MailboxAddress(model.FullName ?? string.Empty, model.Email ?? string.Empty));

            // HTML gövdesi
            string body = $@"
                <!DOCTYPE html>
                <html lang=""tr"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>{messageTitle}</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            background-color: #f9f9f9;
                            margin: 0;
                            padding: 0
                        }}
                        .email-wrapper {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff;
                            border-radius: 8px;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
                            overflow: hidden
                        }}
                        .email-header {{
                            background: linear-gradient(135deg, rgba(3, 77, 151, 0.7), rgba(95, 115, 147, 0.7));
                            padding: 20px;
                            text-align: center
                        }}
                        .email-header img {{
                            max-width: 150px
                        }}
                        .email-body {{
                            padding: 30px;
                            color: #333
                        }}
                        .email-body h2 {{
                            margin-top: 0;
                            font-size: 22px;
                            color: #004080;
                            text-align: center
                        }}
                        .email-body table {{
                            width: 100%;
                            margin-top: 20px;
                            font-size: 16px;
                            border-collapse: collapse
                        }}
                        .email-body td {{
                            padding: 10px 5px;
                            vertical-align: top
                        }}
                        .email-body td.label {{
                            font-weight: bold;
                            width: 120px;
                            color: #666
                        }}
                        .email-body td.message-content {{
                            background: linear-gradient(135deg, rgba(3, 77, 151, 0.2), rgba(95, 115, 147, 0.2));
                            backdrop-filter: blur(8px);
                            -webkit-backdrop-filter: blur(8px);
                            border-radius: 10px;
                            padding: 15px;
                            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                            color: #222;
                            white-space: pre-wrap;
                        }}
                        .email-footer {{
                            background-color: #f1f1f1;
                            padding: 15px;
                            text-align: center;
                            font-size: 13px;
                            color: #999;
                        }}
                        @media screen and (max-width: 600px) {{
                            .email-body {{
                                padding: 20px;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class=""email-wrapper"">
                        <div class=""email-header"">
                            <img src=""https://www.askarprofil.com/img/logo.png"" alt=""Askar Profile Logosu"">
                        </div>
                        <div class=""email-body"">
                            <h2>{messageTitle}</h2>
                            <table>
                                <tr><td class=""label"">Ad Soyad:</td><td>{HttpUtility.HtmlEncode(model.FullName ?? string.Empty)}</td></tr>
                                <tr><td class=""label"">E-posta:</td><td><a href=""mailto:{model.Email ?? string.Empty}"" style=""color: #004080;"">{HttpUtility.HtmlEncode(model.Email ?? string.Empty)}</a></td></tr>
                                <tr><td class=""label"">Telefon:</td><td>{HttpUtility.HtmlEncode(model.Phone ?? "Belirtilmemis")}</td></tr>

                                {tableRows}

                            </table>
                        </div>
                        <div class=""email-footer"">
                            Bu e-posta,KOZLOW E-Ticaret iletisim formu araciligiyla gönderilmistir.<br />
                            &copy; {DateTime.Now.Year} KOZLOW E-Ticaret. Tüm haklari saklidir.
                        </div>
                    </div>
                </body>
                </html>";

            emailMessage.Body = new TextPart("html") { Text = body };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(smtpInfo.Host, smtpInfo.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(smtpInfo.Mail, smtpInfo.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("E-posta gönderimi sirasinda hata olustu.", ex);
            }
        }

        public async Task SendTwoFactorCodeAsync(string email, string code)
        {
            var smtpInfo = await GetSmtpInfoAsync();
            var subject = "Yönetici Girisi Dogrulama Kodu";
            var body = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;font-size:16px;color:#333'>
                    <h2 style='color:#004080'>Giris dogrulamasi</h2>
                    <p>Yönetici paneline girmek icin asagidaki dogrulama kodunu kullanin.</p>
                    <p style='font-size:28px;letter-spacing:4px;font-weight:bold;color:#d32f2f'>{HttpUtility.HtmlEncode(code)}</p>
                    <p style='font-size:14px;color:#666'>Kod 5 dakika icinde kullanilmalidir.</p>
                </div>";

            await SendSimpleMailAsync(smtpInfo, email, subject, body);
        }

        public async Task SendPasswordResetLinkAsync(string email, string resetLink)
        {
            var smtpInfo = await GetSmtpInfoAsync();
            var subject = "Sifre sifirlama baglantisi";
            var safeLink = HttpUtility.HtmlEncode(resetLink);
            var body = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;font-size:16px;color:#333'>
                    <h2 style='color:#004080'>Sifrenizi sifirlayin</h2>
                    <p>Yeni sifre olusturmak icin asagidaki baglantiya tiklayin. Baglanti 30 dakika icinde kullanilmalidir.</p>
                    <p><a href='{safeLink}' style='background:#004080;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;'>Sifreyi sifirla</a></p>
                    <p style='font-size:14px;color:#666'>Eger bu islemi siz baslatmadiysaniz bu e-postayi yok sayabilirsiniz.</p>
                </div>";

            await SendSimpleMailAsync(smtpInfo, email, subject, body);
        }

        private record SmtpInfo(string Mail, string Host, int Port, string Password);

        private async Task<SmtpInfo> GetSmtpInfoAsync()
        {
            var smtpConfig = _configuration.GetSection("SmtpSettings");

            string? host = smtpConfig["Host"];
            string? mail = smtpConfig["Mail"];
            string? password = smtpConfig["Password"];
            string? portStr = smtpConfig["Port"];

            if (!string.IsNullOrWhiteSpace(host) &&
                !string.IsNullOrWhiteSpace(mail) &&
                !string.IsNullOrWhiteSpace(password) &&
                int.TryParse(portStr, out int sitePort))
            {
                return new SmtpInfo(mail, host, sitePort, password);
            }

            // Yedek: appsettings altında MailSettings
            var configHost = _configuration["MailSettings:Host"];
            var configMail = _configuration["MailSettings:Mail"];
            var configPassword = _configuration["MailSettings:Password"];
            var configPort = _configuration["MailSettings:Port"];

            if (!string.IsNullOrWhiteSpace(configHost) &&
                !string.IsNullOrWhiteSpace(configMail) &&
                !string.IsNullOrWhiteSpace(configPassword) &&
                int.TryParse(configPort, out int cfgPort))
            {
                return new SmtpInfo(configMail, configHost, cfgPort, configPassword);
            }

            throw new InvalidOperationException("SMTP ayarlari eksik: panelden veya appsettings.json'dan bilgiler okunamadi.");
        }

        private static async Task SendSimpleMailAsync(SmtpInfo smtpInfo, string toEmail, string subject, string body)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Askar Profile", smtpInfo.Mail));
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpInfo.Host, smtpInfo.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(smtpInfo.Mail, smtpInfo.Password);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
    }
}
