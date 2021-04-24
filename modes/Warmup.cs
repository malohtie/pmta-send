using NLog;
using Send.helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using port25.pmta.api.submitter;
using System.Threading;

namespace Send.modes
{
    class Warmup
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Id { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Username { get; set; }
        public int Sleep { get; set; }
        public int Loop { get; set; }
        public List<dynamic> Servers { get; set; }

        public Warmup(dynamic data)
        {
            Id = !string.IsNullOrWhiteSpace((string)data.id) ? (string)data.id : "";
            Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "";
            Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            Loop = data.loop;
            Sleep = data.sleep;
            Servers = new List<dynamic>(data.servers) ?? throw new ArgumentNullException(nameof(data.servers));
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
                                string vmta = ip.vmta;                            

                                for (int i = 0; i < Loop; i++)
                                {
                                    foreach (string email in Emails)
                                    {
                                        string emailName = email.Split('@')[0];
                                        string boundary = Text.Random("[rndlu/30]");
                                        string bnd = Text.Boundary(Header);
                                        string hd = Text.ReplaceBoundary(Header);
                                        string rp = Text.Build_rp(Return_path, domain, rdns, emailName, "", "", (string)ip.idi, (string)ip.idd, (string)ip.ids, (string)server.name);
                                        hd = Text.Build_header(Header, email_ip, (string)server.name, domain, rdns, email, emailName, boundary, bnd, "", "", (string)ip.idi, (string)ip.idd, (string)ip.ids);
                                        hd = Text.Inject_header(hd, "t", "0", Username, email_ip, (string)ip.idd);
                                        string bd = Text.Build_body(Body, email_ip, (string)server.name, domain, rdns, email, emailName, null, null, null, boundary, bnd, "", "", (string)ip.idi, (string)ip.idd, (string)ip.ids);
                                        Message Message = new Message(rp);
                                        Message.AddData(Text.ReplaceBoundary(hd + "\n" + bd + "\n\n", bnd));
                                        Message.AddRecipient(new Recipient(email));
                                        Message.VirtualMTA = vmta;
                                        Message.JobID = $"0_WARMUP_{Username}_{Id}";
                                        Message.EnvID = Id;
                                        Message.Verp = false;
                                        Message.Encoding = Encoding.EightBit;
                                        p.Send(Message);
                                        Thread.Sleep(Sleep * 1000); //sleep
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
