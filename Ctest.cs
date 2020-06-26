using NLog;
using port25.pmta.api.submitter;
using send.helpers;
using System;
using System.Collections.Generic;

namespace send
{
    class Ctest
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public Message Message { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Mta { get; set; }
        public string Username { get; set; }
        public string Redirect { get; set; }
        public string Unsubscribe { get; set; }
        public string Platform { get; set; }
        public dynamic Servers { get; set; }

        public int Id { get; set; }

        public Ctest(dynamic data)
        {
            Id = data.id ?? 0;
            Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "[rnd]@[domain]";
            Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            Mta = data.mta ?? throw new ArgumentNullException(nameof(data.mta));
            Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            Servers = data.servers ?? throw new ArgumentNullException(nameof(data.servers));
            Redirect = data.redirect ?? throw new ArgumentNullException(nameof(data.redirect));
            Unsubscribe = data.unsubscribe ?? throw new ArgumentNullException(nameof(data.unsubscribe));
            Platform = data.platform ?? throw new ArgumentNullException(nameof(data.platform));
        }

        public List<string> Send()
        {
            List<string> data = new List<string>();
            Random random = new Random();

            foreach (dynamic server in Servers)
            {
                try
                {
                    Pmta p = new Pmta((string)server.mainip, (string)server.password, (string)server.username, (int)server.port);
                    foreach (dynamic ip in server.ips)
                    {
                        string email_ip = ip.ip;
                        string domain = ip.domain;
                        string rdns = Text.Rdns(email_ip, domain);
                        string vmta_ip = email_ip.Replace(':', '.');
                        string vmta = Mta.ToLower() == "none" ? $"mta-{vmta_ip}" : (Mta == "vmta" ? $"vmta-{vmta_ip}-{Username}" : $"smtp-{vmta_ip}-{Username}");
                        string job = $"0_CAMPAIGN-TEST_{Id}_{Username}";


                        string key = Text.Adler32($"{Id}0");
                        string redirect = Text.Base64Encode($"{Id}-0-{key}-{random.Next(1000, 99999)}");
                        string unsubscribe = Text.Base64Encode($"{Id}-0-{key}-{random.Next(1000, 99999)}");
                        string open = Text.Base64Encode($"{Id}-0-{key}-{random.Next(1000, 99999)}");


                        foreach (string email in Emails)
                        {
                            string boundary = Text.Random("[rndlu/30]");
                            string emailName = email.Split('@')[0];
                            string rp = Text.Build_rp(Return_path, domain, rdns, emailName);
                            string hd = Text.Build_header(Header, email_ip, domain, rdns, email, emailName, boundary);
                            hd = Text.Inject_header(hd, "t", Id.ToString(), Username, Convert.ToString(ip.ip), Convert.ToString(ip.idddomain));
                            string bd = Text.Build_body(Body, email_ip, domain, rdns, email, emailName, boundary);
                            bd = Text.Generate_links(bd, redirect, unsubscribe, open);
                            Message = new Message(rp);
                            Message.AddData(hd + "\n" + bd + "\n\n");
                            Message.AddRecipient(new Recipient(email));
                            Message.VirtualMTA = vmta;
                            Message.JobID = job;
                            Message.Verp = false;
                            Message.Encoding = Encoding.EightBit;
                            p.Send(Message);
                        }
                    }
                    data.Add($"SERVER {server.mainip} OK");
                    p.Close();
                }
                catch (Exception ex)
                {
                    data.Add($"ERROR SERVER {server.mainip} - {ex.Message}");
                    logger.Error(ex.Message);
                }
            }
            return data;
        }

    }
}
