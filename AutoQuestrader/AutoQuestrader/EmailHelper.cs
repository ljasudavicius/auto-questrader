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
        public static string UserEmailPassword;
        public static readonly string SETTING_NAME_ACCOUNT_OWNER_EMAIL_ADDRESS = "ACCOUNT_OWNER_EMAIL_ADDRESS";
        public static readonly string SETTING_NAME_ACCOUNT_OWNER_EMAIL_DISPLAY_NAME = "ACCOUNT_OWNER_EMAIL_DISPLAY_NAME";

        public static void SendEmail(string subject, string body, MailAddress toAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(UserEmailPassword))
                {
                    Console.WriteLine("Please enter the password for: " + GetAccountOwnerEmail().Address);
                    Console.WriteLine("(You should read the source code to be assured that your password is not used nefariously.)");

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
                    Credentials = new NetworkCredential(GetAccountOwnerEmail().Address, UserEmailPassword)
                };
                using (var message = new MailMessage(GetAccountOwnerEmail(), toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch {
                Console.WriteLine("Error sending email. Make use the credentials are correct. If using gmail you may need to allow this app: https://support.google.com/accounts/answer/6010255?hl=en-GB");
                SendEmail(subject, body, toAddress);
            }
        }

        public static void SendNorbertsGambitEmail(string accountNumber, int quantity)
        {
            string NGSubject = "Journal " + Program.NG_SYMBOL_CAD + " to " + Program.NG_SYMBOL_USD;
            string NGBody = "Hello,\r\n\r\n" +
               "I just purchased some shares of " + Program.NG_SYMBOL_CAD + ".\r\n" +
               "When available, could you please journal " + quantity + " shares of " + Program.NG_SYMBOL_CAD + " to " + Program.NG_SYMBOL_USD + " on Account# " + accountNumber + "\r\n" +
               "I am aware of the processing times involved and am ok with it.\r\n\r\n" +
               "Thank you,\r\n" +
               GetAccountOwnerEmail().DisplayName;

            SendEmail(NGSubject, NGBody, new MailAddress("support@questrade.com", "Questrade Support"));
        }

        public static MailAddress GetAccountOwnerEmail() {

            AutoQuestraderEntities db = new AutoQuestraderEntities();
            var ownerEmailSetting = db.SettingValues.FirstOrDefault(p => p.Name == SETTING_NAME_ACCOUNT_OWNER_EMAIL_ADDRESS);
            string ownerEmail;
            if (ownerEmailSetting == null || string.IsNullOrEmpty(ownerEmailSetting.Value))
            {
                Console.WriteLine("Please enter the email address of the account owner:");
                ownerEmail = Console.ReadLine().Trim();
                db.SettingValues.Add(new SettingValue {
                    Name= SETTING_NAME_ACCOUNT_OWNER_EMAIL_ADDRESS,
                    Value= ownerEmail
                });
                db.SaveChanges();
            }
            else{
                ownerEmail = ownerEmailSetting.Value;
            }

            var ownerDisplayNameSetting = db.SettingValues.FirstOrDefault(p => p.Name == SETTING_NAME_ACCOUNT_OWNER_EMAIL_DISPLAY_NAME);
            string ownerDisplayName;
            if (ownerDisplayNameSetting == null || string.IsNullOrEmpty(ownerDisplayNameSetting.Value))
            {
                Console.WriteLine("Please enter the display name for email address: "+ ownerEmail);
                ownerDisplayName = Console.ReadLine().Trim();
                db.SettingValues.Add(new SettingValue
                {
                    Name = SETTING_NAME_ACCOUNT_OWNER_EMAIL_DISPLAY_NAME,
                    Value = ownerDisplayName
                });
                db.SaveChanges();
            }
            else
            {
                ownerDisplayName = ownerDisplayNameSetting.Value;
            }

            return new MailAddress(ownerEmail, ownerDisplayName);
        }

    }
}
