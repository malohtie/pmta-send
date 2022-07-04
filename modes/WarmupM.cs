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
    class WarmupM
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Id { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Username { get; set; }
        public int Skip { get; set; }
        public int Limit { get; set; }
        public int Sleep { get; set; }
        public int Sleep_loop { get; set; }
        public int Start_after { get; set; }
        public bool Random_emails { get; set; }
        public int Loop { get; set; }
        public List<dynamic> Servers { get; set; }
        public string SendId { get; set; }

        public WarmupM(dynamic data)
        {
            Id = !string.IsNullOrWhiteSpace((string)data.id) ? (string)data.id : "";
            Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "";
            Emails = data.emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            Skip = data.skip ?? 0;
            Limit = data.limit ?? 100;
            Loop = data.loop;
            Sleep = data.sleep;
            Sleep_loop = data.sleep_loop;
            Start_after = data.start_after;
            Random_emails = Convert.ToBoolean(data.random_emails);
            Servers = new List<dynamic>(data.servers) ?? throw new ArgumentNullException(nameof(data.servers));
            SendId = !string.IsNullOrWhiteSpace((string)data.send_id) ? (string)data.send_id : "0";
            Emails = Emails.Skip(Skip).Take(Limit).ToArray();
        }

        public List<string> Send()
        {
            List<string> data = new List<string>();
            if (Emails?.Length > 0)
            {
                List<Task> tasks = new List<Task>();
                Random random = new Random();
                if (Random_emails)
                {
                    Emails = Emails.OrderBy(item => random.Next()).ToArray();
                }
                foreach (dynamic server in Servers)
                {
                    tasks.Add(
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                Thread.Sleep(Start_after * 1000); //start after
                                Pmta p = new Pmta((string)server.mainip, (string)server.password, (string)server.username, (int)server.port);
                                Parallel.ForEach((IEnumerable<dynamic>)server.ips, ip =>
                                {
                                    string email_ip = ip.ip;
                                    string domain = ip.domain;
                                    string rdns = Text.Rdns(email_ip, domain);
                                    string vmta = ip.vmta;

                                    for (int i = 0; i < Loop; i++)
                                    {
                                        string rp = Text.Build_rp(Return_path, domain, rdns, "info");
                                        Message message = new Message(rp);
                                        string header = Text.Header_normal(Header);
                                        string genB = Text.ReplaceBoundary(header + "\n" + Body + "\n\n");
                                        message.AddMergeData(Text.Generate(genB));
                                        message.VirtualMTA = vmta;
                                        message.JobID = $"0_WARMUPM_{Username}_{Id}";
                                        message.EnvID = Id;
                                        message.Verp = false;
                                        message.Encoding = Encoding.EightBit;
                                        foreach (string email in Emails)
                                        {
                                            Recipient t = new Recipient(email);
                                            string tkey = Text.Adler32($"{Id}0");
                                            t["red"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" +$"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]"));
                                            t["unsub"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" +$"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]"));
                                            t["opn"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" +$"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]"));

                                            t["pe"] = $"w,{(string)ip.ids},{Username},{email_ip},{(string)ip.idd},0";
                                            t["ip"] = email_ip;
                                            t["server"] = (string)server.name + (string)ip.ids;
                                            t["domain"] = domain;
                                            t["idi"] = (string)ip.idi;
                                            t["idd"] = (string)ip.idd;
                                            t["ids"] = (string)ip.ids;
                                            t["ide"] = "0";
                                            t["rdns"] = rdns;
                                            t["name"] = email.Split('@')[0];
                                            t["to"] = email;
                                            t["reply"] = email;
                                            t["date"] = Text.GetRFC822Date();
                                            t["boundary"] = Text.Random("[rndlu/30]");
                                            t["bnd"] = Text.Boundary(header);
                                            t["*parts"] = "1";
                                            message.AddRecipient(t);
                                        }
                                        p.Send(message);
                                        Thread.Sleep(Sleep_loop * 1000); //sleep loop
                                    }
                                });
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
            }
            return data;
        }
    }
}
