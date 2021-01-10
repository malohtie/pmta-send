using NLog;
using port25.pmta.api.submitter;
using Send.helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Send.modes
{
    class GlobalTest
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public bool IsNegative { get; set; }
        public string Negative { get; set; }
        public string Mta { get; set; }
        public string Option { get; set; }
        public string Username { get; set; }
        public int Repeat { get; set; }
        public List<dynamic> Servers { get; set; }

        public GlobalTest(dynamic data)
        {
            this.Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "";
            this.Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            this.Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            this.Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            this.Mta = data.mta ?? throw new ArgumentNullException(nameof(data.mta));
            this.Option = Convert.ToString(data.option) ?? "ip";
            this.Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            this.Repeat = data.repeat ?? 1;
            this.Servers = new List<dynamic>(data.servers) ?? throw new ArgumentNullException(nameof(data.servers));
            IsNegative = Convert.ToString(data.is_negative) == "1";
            if (IsNegative)
            {
                Campaign cam = new Campaign(Convert.ToString(data.artisan));
                Negative = cam.Campaign_negative(Convert.ToString(data.negative));
            }
        }

        public List<string> Send()
        {
            List<string> data = new List<string>();
            List<Task> tasks = new List<Task>();
            foreach (dynamic server in Servers)
            {
                tasks.Add(
                    Task.Factory.StartNew(() =>
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
                                if (this.Option == "vmta")
                                {
                                    vmta = $"mta-{vmta_ip}-{ip.cmta}";
                                }
                                string job = $"0_GLOBAL-TEST_{Username}";

                                if (IsNegative)
                                {
                                    Body = Text.Build_negative(Body, Negative);
                                }

                                for (int i = 0; i < this.Repeat; i++)
                                {
                                    foreach (string email in Emails)
                                    {
                                        string emailName = email.Split('@')[0];
                                        string boundary = Text.Random("[rndlu/30]");
                                        string bnd = Text.boundary(Header);
                                        string hd = Text.replaceBoundary(Header);
                                        string rp = Text.Build_rp(Return_path, domain, rdns, emailName);
                                        hd = Text.Build_header(Header, email_ip, (string)server.id, domain, rdns, email, emailName, boundary, bnd);
                                        hd = Text.Inject_header(hd, "t", "0", Username, email_ip, Convert.ToString(ip.idddomain));
                                        string bd = Text.Build_body(Body, email_ip, (string)server.id, domain, rdns, email, emailName, null, null, null, boundary, bnd);
                                        Message Message = new Message(rp);
                                        Message.AddData(Text.replaceBoundary(hd + "\n" + bd + "\n\n", bnd));
                                        Message.AddRecipient(new Recipient(email));
                                        Message.VirtualMTA = vmta;
                                        Message.JobID = job;
                                        Message.Verp = false;
                                        Message.Encoding = Encoding.EightBit;
                                        p.Send(Message);
                                    }
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
                    })
                );
            }
            Task.WaitAll(tasks.ToArray());
            return data;
        }
    }
}
