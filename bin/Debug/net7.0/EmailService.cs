using System;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using business_logic_layer.ViewModel;
using MimeKit;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace business_logic_layer
{

    public class EmailService : IEmailService
    {
        private readonly emailSettingsModel _emailSettings;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;



        public EmailService(IOptions<emailSettingsModel> options, IWebHostEnvironment env, IConfiguration configuration)
        {
            _emailSettings = options.Value;
            _configuration = configuration;
            _environment = env;

        }
        //EmailTemplateSofani
        public async Task SendEmailAsync(mailRequestModel mailRequest)
        {
            var emailTemplatePathSofani = _configuration["TemplatePaths:EmailTemplateSofani"];
            var emailTemplatePath = Path.Combine(_environment.WebRootPath, emailTemplatePathSofani);
            var emailTemplate = await File.ReadAllTextAsync(emailTemplatePath);
            emailTemplate = emailTemplate.Replace("{OrderDate}", mailRequest.OrderDate.ToString());
            emailTemplate = emailTemplate.Replace("{recipientName}", mailRequest.recipientName);
            emailTemplate = emailTemplate.Replace("{line1}", mailRequest.line1);
            emailTemplate = emailTemplate.Replace("{city}", mailRequest.city);
            emailTemplate = emailTemplate.Replace("{paymentMethodType}", mailRequest.paymentMethodType);
            emailTemplate = emailTemplate.Replace("{OrderNummer}", mailRequest.OrderNummer.ToString());
            emailTemplate = emailTemplate.Replace("{postalCode}", mailRequest.postalCode);

            var confirmationEmail = new MimeMessage();
            confirmationEmail.Sender = MailboxAddress.Parse(_emailSettings.SenderEmail);
            confirmationEmail.To.Add(MailboxAddress.Parse(mailRequest.CustomerName));
            confirmationEmail.Subject = "Confirmation of your order";
            var confirmationBuilder = new BodyBuilder();
            decimal AmountTotal = 0;
            StringBuilder orderItemsBuilder = new StringBuilder();
            foreach (var item in mailRequest.OrderItems) 
            {
                decimal price = item.Price;
                decimal total = item.Total;
                AmountTotal += total;
                string formattedPrice = price.ToString("0.00", new System.Globalization.CultureInfo("nl-NL"));
                string formattedTotal = total.ToString("0.00", new System.Globalization.CultureInfo("nl-NL"));

                orderItemsBuilder.AppendLine($@"
        <tr>
            <td style='padding: 8px; border-bottom: 1px solid #e0e0e0;'>
                <img src='{item.ImageUrl}' alt='Product Image' width='50px' height='50px' />
            </td>
            <td style='padding: 8px; border-bottom: 1px solid #e0e0e0;'>{item.ProductName}</td>
            <td style='padding: 8px; border-bottom: 1px solid #e0e0e0;'>{item.Quantity}</td>
            <td style='padding: 8px; border-bottom: 1px solid #e0e0e0;'>€{formattedPrice}</td>
            <td style='padding: 8px; border-bottom: 1px solid #e0e0e0;'>€{formattedTotal}</td>
        </tr>");
            }
            string orderItemsHtml = orderItemsBuilder.ToString();
            string formattedAmountTotal = AmountTotal.ToString("0.00", new System.Globalization.CultureInfo("nl-NL"));
            emailTemplate = emailTemplate.Replace("{formattedAmountTotal}", formattedAmountTotal);
            emailTemplate = emailTemplate.Replace("{orderItemsHtml}", orderItemsHtml);
            confirmationBuilder.HtmlBody = emailTemplate;
            confirmationEmail.Body = confirmationBuilder.ToMessageBody();

            await SendMimeMessageAsync(confirmationEmail);
            var adminNotificationEmail = new MimeMessage();
            adminNotificationEmail.Sender = MailboxAddress.Parse(_emailSettings.SenderEmail);
            adminNotificationEmail.To.Add(MailboxAddress.Parse(_emailSettings.SenderEmail)); 
            adminNotificationEmail.Subject = $"Nieuwe bestelling ontvangen: {mailRequest.OrderNummer}";

            var adminBuilder = new BodyBuilder();
            
            adminBuilder.HtmlBody = $@"
             <h1>Nieuwe Bestelling Ontvangen</h1>
             <p><strong>Bestelnummer:</strong> {mailRequest.OrderNummer}</p>
             <p><strong>Klantnaam:</strong> {mailRequest.recipientName}</p>
             <p><strong>E-mail:</strong> {mailRequest.CustomerName}</p>
            <p><strong>Adres:</strong> {mailRequest.line1}, {mailRequest.city}, {mailRequest.postalCode}</p>
             <p><strong>Besteldatum:</strong> {mailRequest.OrderDate.ToString("dd-MM-yyyy")}</p>";

            adminNotificationEmail.Body = adminBuilder.ToMessageBody();

            await SendMimeMessageAsync(adminNotificationEmail);

        }


        private async Task SendMimeMessageAsync(MimeMessage message)
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderName, _emailSettings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }

}

