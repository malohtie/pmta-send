using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Send.modes
{
    class SmtpTest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Route { get; set; }
        public int Port { get; set; }
        public string Test { get; set; }
        public SmtpTest(dynamic data)
        {
            Email = (string)data["email"];
            Password = (string)data["password"];
            Route = (string)data["route"];
            Port = int.Parse((string)data["port"]);
            Test = (string)data["test"];
        }

        public List<string> Send()
        {
            List<string> result = new List<string>();
            try
            {
                SmtpClient smtp = new SmtpClient(Route, Port);
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(Email, "aaa");
                string account = Email.Split('@')[0];
                string guid = Guid.NewGuid().ToString("n");
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(Email, account);
                mail.Subject = $"{account} - {guid}";
                mail.IsBodyHtml = true;
                mail.Body = "";
                mail.To.Add(Test);
                smtp.Send(mail);
                result.Add("OK SENDED");              
            }
            catch (Exception ex)
            {
                result.Add(ex.Message.ToString());
            }
            return result;
        }
    }
}
