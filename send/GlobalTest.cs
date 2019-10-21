using NLog;
using port25.pmta.api.submitter;
using send.helpers;
using System;
using System.Collections.Generic;

namespace send
{
    class GlobalTest
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public Message Message { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Mta { get; set; }
        public string Username { get; set; }
        public dynamic Servers { get; set; }

        public GlobalTest(dynamic data)
        {
            this.Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "[rnd]@[domain]"; 
            this.Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            this.Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            this.Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            this.Mta = data.mta ?? throw new ArgumentNullException(nameof(data.mta));
            this.Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            this.Servers = data.servers ?? throw new ArgumentNullException(nameof(data.servers));
        }

        public GlobalTest(string return_path, string[] emails, string header, string body, string mta, string username, dynamic servers)
        {
            this.Return_path = !string.IsNullOrWhiteSpace(return_path) ? return_path : "[rnd]@[domain]";
            this.Emails = emails ?? throw new ArgumentNullException(nameof(emails));
            this.Header = Text.Base64Decode(header) ?? throw new ArgumentNullException(nameof(header));
            this.Body = Text.Base64Decode(body) ?? "";
            this.Mta = mta ?? throw new ArgumentNullException(nameof(mta));
            this.Username = username ?? throw new ArgumentNullException(nameof(username));
            this.Servers = servers ?? throw new ArgumentNullException(nameof(servers));
        }

        public List<string> Send()
        {
            List<string> data = new List<string>(); 
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
                        string job = $"0_GLOBAL-TEST_{Username}";

                        foreach (string email in Emails)
                        {
                            string emailName = email.Split('@')[0];
                            string rp = Text.Build_rp(Return_path, domain, rdns, emailName);
                            string hd = Text.Build_header(Header, email_ip, domain, rdns, email, emailName);
                            hd = Text.Inject_header(hd, "t", "0", Username, ip.idip, ip.idddomain);
                            string bd = Text.Build_body(Body, email_ip, domain, rdns, email, emailName);
                            Message = new Message(rp);
                            Message.AddData(hd + "\n" + bd);
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
