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
        public string Id { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public bool IsNegative { get; set; }
        public string Negative { get; set; }
        public string Username { get; set; }
        public int Repeat { get; set; }
        public List<dynamic> Servers { get; set; }

        public GlobalTest(dynamic data)
        {
            Id = !string.IsNullOrWhiteSpace((string)data.id) ? (string)data.id : "";
            Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "";
            Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.test_emails));
            Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            Repeat = data.repeat ?? 1;
            Servers = new List<dynamic>(data.servers) ?? throw new ArgumentNullException(nameof(data.servers));
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
                                string account = "";
                                try
                                {
                                    account = (string)ip["from"] ?? "";
                                }
                                catch
                                {
                                    account = "";
                                }

                                string email_ip = ip.ip;
                                string domain = ip.domain;
                                string rdns = Text.Rdns(email_ip, domain);
                                string vmta = ip.vmta;
                                string route = ip?.route; // safe access in case ip is null
                                string route_alias = null;
                                string route_domain = null;
                                if (!string.IsNullOrEmpty(route) && route.Contains("@"))
                                {
                                    var parts = route.Split('@');
                                    if (parts.Length == 2)
                                    {
                                        route_alias = parts[0];
                                        route_domain = parts[1];
                                    }
                                }

                                string job = $"0_GLOBAL-TEST_{Username}_{Id}";

                                if (IsNegative)
                                {
                                    Body = Text.Build_negative(Body, Negative);
                                }

                                for (int i = 0; i < Repeat; i++)
                                {
                                    foreach (string email in Emails)
                                    {
                                        string emailName = email.Split('@')[0];
                                        string boundary = Text.Random("[rndlu/30]");
                                        string bnd = Text.Boundary(Header);
                                        string hd = Text.ReplaceBoundary(Header);
                                        string rp = Text.Build_rp(Return_path, domain, rdns, emailName, "", "", (string)ip.idi, (string)ip.idd, (string)ip.ids, (string)server.name, account, route, route_alias, route_domain);
                                        hd = Text.Build_header(Header, email_ip, (string)server.name, domain, rdns, email, emailName, boundary, bnd, "", (string)ip.idi, (string)ip.idd, (string)ip.ids, "0", account, route, route_alias, route_domain);
                                        hd = Text.Inject_header(hd, "t", "0", Username, email_ip, (string)ip.idd);
                                        string bd = Text.Build_body(Body, email_ip, (string)server.name, domain, rdns, email, emailName, null, null, null, boundary, bnd, "", (string)ip.idi, (string)ip.idd, (string)ip.ids, "0", account, route, route_alias, route_domain);
                                        Message Message = new Message(rp);
                                        Message.AddData(Text.ReplaceBoundary(hd + "\n" + bd + "\n\n", bnd));
                                        Message.AddRecipient(new Recipient(email));
                                        Message.VirtualMTA = vmta;
                                        Message.JobID = job;
                                        Message.EnvID = Id;
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
