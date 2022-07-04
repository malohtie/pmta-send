using NLog;
using port25.pmta.api.submitter;
using Send.helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Send.modes
{
    class Ctest
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Id { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Username { get; set; }
        public string TestId { get; set; }
        public string Redirect { get; set; }
        public string Unsubscribe { get; set; }
        public string Platform { get; set; }
        public List<dynamic> Servers { get; set; }
        public bool IsPlaceHolder { get; set; }
        public Placeholder Placeholder { get; set; }
        public bool IsAutoReply { get; set; }
        public Rotation Reply { get; set; }       
        public bool IsNegative { get; set; }
        public string Negative { get; set; }
        public Ctest(dynamic data)
        {
            Id = data.id ?? throw new ArgumentNullException(nameof(data.id));
            Return_path = !string.IsNullOrWhiteSpace((string)data.return_path) ? (string)data.return_path : "";
            Emails = data.test_emails.ToObject<string[]>() ?? throw new ArgumentNullException(nameof(data.emails));
            Header = Text.Base64Decode(Convert.ToString(data.header)) ?? throw new ArgumentNullException(nameof(data.header));
            Body = Text.Base64Decode(Convert.ToString(data.body)) ?? "";
            Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            TestId = !string.IsNullOrWhiteSpace((string)data.test_id) ? (string)data.test_id : "0";
            Servers = new List<dynamic>(data.servers) ?? throw new ArgumentNullException(nameof(data.servers));
            IsAutoReply = Convert.ToString(data.is_auto_reply) == "1";
            if (IsAutoReply)
            {
                Reply = new Rotation(data.auto_reply_data, (int)data.auto_reply_every);
            }
            IsPlaceHolder = Convert.ToString(data.is_placeholder) == "1";
            if(IsPlaceHolder)
            {
                Placeholder = new Placeholder(data.placeholder_data, (int)data.placeholder_every);
            }
            IsNegative = Convert.ToString(data.is_negative) == "1";
            if (IsNegative && !string.IsNullOrEmpty((string)data.negative))
            {
                Campaign cam = new Campaign(Convert.ToString(data.artisan));
                Negative = cam.Campaign_negative(Convert.ToString(data.negative));
            }
        }

        public List<string> Send()
        {
            List<string> data = new List<string>();
            List<Task> tasks = new List<Task>();
            Random random = new Random();
            int placeholder_counter = 1;
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
                                    account = (string)ip.from ?? "";
                                }
                                catch {

                                    account = "";
                                }

                                string email_ip = ip.ip;
                                string domain = ip.domain;
                                string rdns = Text.Rdns(email_ip, domain);
                                string vmta = ip.vmta;
                                string job = $"0_CAMPAIGN-TEST_{Id}_{Username}";


                                string key = Text.Adler32($"{Id}0");
                                string redirect = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-0-{key}-{TestId}-" + Text.Random("[rnda/20]"));
                                string unsubscribe = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-0-{key}-{TestId}-" + Text.Random("[rnda/20]"));
                                string open = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-0-{key}-{TestId}-" + Text.Random("[rnda/20]"));

                                if (IsNegative)
                                {
                                    Body = Text.Build_negative(Body, Negative);
                                }
                                foreach (string email in Emails)
                                {                                
                                    string currentEmail = IsAutoReply ? Reply.ThreadGetAndRotate() : email;
                                    string boundary = Text.Random("[rndlu/30]");
                                    string bnd = Text.Boundary(Header);
                                    string hd = Text.ReplaceBoundary(Header);
                                    string bd = Text.ReplaceBoundary(Body);
                                    string emailName = email.Split('@')[0];
                                    string rp = Text.Build_rp(Return_path, domain, rdns, emailName, currentEmail, (string)ip.idi, (string)ip.idd, (string)ip.ids, (string)server.name, email, account);
                                    rp = IsPlaceHolder ? Placeholder.ReplaceRotate(rp, placeholder_counter, true) : rp;
                                    hd = Text.Build_header(hd, email_ip, (string)server.name, domain, rdns, email, emailName, boundary, bnd, currentEmail, (string)ip.idi, (string)ip.idd, (string)ip.ids, "0", account);
                                    hd = IsPlaceHolder ? Placeholder.ReplaceRotate(hd, placeholder_counter, true) : hd;
                                    hd = Text.Inject_header(hd, "t", Id, Username, Convert.ToString(ip.ip), Convert.ToString(ip.idd));
                                    bd = Text.Build_body(bd, email_ip, (string)server.id, domain, rdns, email, emailName, redirect, unsubscribe, open, boundary, bnd, currentEmail, (string)ip.idi, (string)ip.idd, (string)ip.ids, "0", account);
                                    bd = IsPlaceHolder ? Placeholder.ReplaceRotate(bd, placeholder_counter, true) : bd;
                                    Message Message = new Message(rp);
                                    Message.AddData(Text.ReplaceBoundary(hd + "\n" + bd + "\n\n", bnd));
                                    Message.AddRecipient(new Recipient(currentEmail));
                                    Message.VirtualMTA = vmta;
                                    Message.JobID = job;
                                    Message.EnvID = Id;
                                    Message.Verp = false;
                                    Message.Encoding = Encoding.EightBit;
                                    p.Send(Message);
                                    Interlocked.Increment(ref placeholder_counter);
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
