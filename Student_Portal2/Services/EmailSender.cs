using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Student_Portal2.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    "usamahamid324@gmail.com",
                    "zsxzrditfsvbiwks"
                ),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("usamahamid324@gmail.com"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
