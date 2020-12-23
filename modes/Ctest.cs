﻿using NLog;
using port25.pmta.api.submitter;
using send.helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Send.modes
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
        public string Option { get; set; }
        public string Username { get; set; }
        public string Redirect { get; set; }
        public string Unsubscribe { get; set; }
        public string Platform { get; set; }
        public dynamic Servers { get; set; }

        public int Id { get; set; }

        public Ctest(dynamic data)
        {
            Id = data.id ?? 0;
            Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "";
            Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            Mta = data.mta ?? throw new ArgumentNullException(nameof(data.mta));
            Option = Convert.ToString(data.option) ?? "ip";
            Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            Servers = data.servers ?? throw new ArgumentNullException(nameof(data.servers));
            Redirect = data.redirect ?? throw new ArgumentNullException(nameof(data.redirect));
            Unsubscribe = data.unsubscribe ?? throw new ArgumentNullException(nameof(data.unsubscribe));
            Platform = data.platform ?? throw new ArgumentNullException(nameof(data.platform));
        }

        public List<string> Send()
        {
            List<string> data = new List<string>();
            List<Task> tasks = new List<Task>();
            Random random = new Random();

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
                                if (Option == "vmta")
                                {
                                    vmta = $"mta-{vmta_ip}-{ip.cmta}";
                                }
                                string job = $"0_CAMPAIGN-TEST_{Id}_{Username}";


                                string key = Text.Adler32($"{Id}0");
                                string redirect = Text.Base64Encode($"{Id}-0-{key}-{random.Next(1000, 99999)}");
                                string unsubscribe = Text.Base64Encode($"{Id}-0-{key}-{random.Next(1000, 99999)}");
                                string open = Text.Base64Encode($"{Id}-0-{key}-{random.Next(1000, 99999)}");


                                foreach (string email in Emails)
                                {
                                    string boundary = Text.Random("[rndlu/30]");
                                    string bnd = Text.boundary(Header);
                                    string hd = Text.replaceBoundary(Header);
                                    string bd = Text.replaceBoundary(Body);
                                    string emailName = email.Split('@')[0];
                                    string rp = Text.Build_rp(Return_path, domain, rdns, emailName);
                                    hd = Text.Build_header(hd, email_ip, (string)server.id, domain, rdns, email, emailName, boundary, bnd);
                                    hd = Text.Inject_header(hd, "t", Id.ToString(), Username, Convert.ToString(ip.ip), Convert.ToString(ip.idddomain));
                                    bd = Text.Build_body(bd, email_ip, (string)server.id, domain, rdns, email, emailName, redirect, unsubscribe, open, boundary, bnd);
                                    Message = new Message(rp);
                                    Message.AddData(Text.replaceBoundary(hd + "\n" + bd + "\n\n", bnd));
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
                    })
                );
            }
            Task.WaitAll(tasks.ToArray());
            return data;
        }

    }
}
