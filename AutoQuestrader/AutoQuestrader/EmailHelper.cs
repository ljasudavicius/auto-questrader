using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace AutoQuestrader
{
    public static class EmailHelper
    {
        public static readonly MailAddress USER_EMAIL = new MailAddress("luke.jasudavicius@gmail.com", "Luke Jasudavicius");
        public static string UserEmailPassword;

        public static void SendEmail(string subject, string body, MailAddress toAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(UserEmailPassword))
                {
                    Console.WriteLine("Please enter the password for: " + USER_EMAIL);
                    while (true)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                            break;
                        UserEmailPassword += key.KeyChar;
                    }
                }

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(USER_EMAIL.Address, UserEmailPassword)
                };
                using (var message = new MailMessage(USER_EMAIL, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch {
                throw new Exception("Error sending email. Make use the credentials are correct. If using gmail you may need to allow this app: https://support.google.com/accounts/answer/6010255?hl=en-GB");
            }
        }

        public static void SendNorbertGambitEmail(string accountNumber, int quantity)
        {
            string NGSubject = "Journal DLR.TO to DLR-U.TO";
            string NGBody = "Hello,\r\n\r\n" +
               "I just purchased some shares of DLR.TO.\r\n" +
               "When available, could you please journal " + quantity + " shares of DLR.TO to DLR-U.TO on Account# " + accountNumber + "\r\n" +
               "I am aware of the processing times involved and am ok with it.\r\n\r\n" +
               "Thank you,\r\n" +
               USER_EMAIL.DisplayName;

            SendEmail(NGSubject, NGBody, new MailAddress("support@questrade.com", "Questrade Support"));
        }

    }
}
