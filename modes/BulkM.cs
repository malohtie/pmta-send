using NLog;
using port25.pmta.api.submitter;
using Send.helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Send.modes
{
    class BulkM
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public int Id { get; set; }
        public int Fraction { get; set; }
        public int Loop { get; set; }
        public int Delay { get; set; }
        public int Sleep { get; set; }
        public int Seed { get; set; }
        public string Artisan { get; set; }
        public string Storage { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string SendId { get; set; }
        public Rotation Reply { get; set; }
        public Placeholder Placeholder { get; set; }
        public string Negative { get; set; }

        public BulkM(dynamic data)
        {
            Id = int.Parse((string)data.campaign);
            Fraction = int.Parse((string)data.fraction);
            Loop = int.Parse((string)data.loop);
            Delay = int.Parse((string)data.delay);
            Sleep = int.Parse((string)data.sleep);
            Seed = int.Parse((string)data.seed);

            Artisan = Convert.ToString(data.artisan) ?? throw new ArgumentNullException(nameof(Artisan));
            Storage = Convert.ToString(data.storage) ?? throw new ArgumentNullException(nameof(Storage));
            Password = Convert.ToString(data.password) ?? throw new ArgumentNullException(nameof(Password));
            Username = Convert.ToString(data.username) ?? throw new ArgumentNullException(nameof(Username));
            SendId = !string.IsNullOrWhiteSpace((string)data.send_id) ? (string)data.send_id : "0";
        }
        public List<string> Send()
        {

            List<string> Result = new List<string>();
            Campaign campaign = new Campaign(Artisan);
            List<Task> tasks = new List<Task>();            
            Random random = new Random();
            int c_seed = 0;
            int placeholder_counter = 1;

            for (int l = 0; l < Loop; l++) //loop
            {
                Console.WriteLine("Loop : " + l);
                try
                {
                    dynamic cdata = campaign.Campaign_info(Id);
                    if (cdata != null)
                    {
                        string raw_rp = Convert.ToString(cdata.return_path) ?? "";
                        string[] seed_emails = Campaign.Convert_emails(Convert.ToString(cdata.email_test));
                        string raw_hd = Text.Base64Decode(Convert.ToString(cdata.header));
                        string raw_bd = Text.Base64Decode(Convert.ToString(cdata.body));
                        var servers = Campaign.Bulk_split(Convert.ToString(cdata.ips));
                        if (servers.Count == 0)
                        {
                            Result.Add("No Servers To Process");
                            logger.Error("No Servers To Process");
                            campaign.Campaign_update_progress(Id, "start", true, 0);
                            return Result;
                        }
                        string file = "/" + Convert.ToString(cdata.send_file);
                        string platform = Convert.ToString(cdata.platform);
                        string redirect_link = Convert.ToString(cdata.redirect_link);
                        string unsubscribe_link = Convert.ToString(cdata.unsubscribe_link);
                        bool IsPlaceholder = Convert.ToString(cdata.is_placeholder) == "1";
                        if (IsPlaceholder)
                        {
                            Placeholder = new Placeholder(cdata.placeholder_data, (int)cdata.placeholder_every);
                        }
                        bool IsAutoReply = Convert.ToString(cdata.is_auto_reply) == "1";
                        if (IsAutoReply)
                        {
                            Reply = new Rotation(cdata.auto_reply_data, (int)cdata.auto_reply_every);
                        }
                        bool IsNegative = Convert.ToString(cdata.is_negative) == "1";
                        if (IsNegative && string.IsNullOrEmpty(Negative))
                        {
                            Negative = campaign.Campaign_negative(Convert.ToString(cdata.negative));
                        }
                        if (IsNegative)
                        {
                            raw_bd = Text.Build_negative(raw_bd, Negative);
                        }

                        string mta = Convert.ToString(cdata.option);
                        int progress = int.Parse(Convert.ToString(cdata.send_progress));
                        int total = int.Parse(Convert.ToString(cdata.send_count));
                        int countIps = servers.Count;

                        if(total - progress <= 0)
                        {
                            campaign.Campaign_update_progress(Id, "finish", true, 0);
                            return Result;
                        }

                        int toSend = countIps * Fraction;
                        if (total - (progress + (countIps*Fraction)) < 0) {
                            toSend = total - progress;
                        }

                        List<int> ipLimit = Campaign.DistributeInteger(toSend, countIps).ToList();
                        for (int i = 0; i < countIps; i++)
                        {
                            int current = i;
                            tasks.Add(
                                 Task.Factory.StartNew(() =>
                                 {
                                     try
                                     {
                                         if (ipLimit[current] > 0)
                                         {
                                             var details_server = campaign.Server_info(int.Parse(servers[current][4]));
                                             if (details_server != null)
                                             {
                                                 Pmta p = new Pmta(Convert.ToString(details_server.ip), Password); //load pmta
                                                 int skip = (current == 0 ? 0 : ipLimit.Take(current).Sum()) + progress;
                                                 Console.WriteLine("skip " + skip);

                                                 List<string[]> emails = File.ReadLines(Storage + file).Skip(skip).Take(ipLimit[current])
                                                                     .Select(t => t.Trim().Split(','))
                                                                     .Where(item => item.Length == 2)
                                                                     .ToList();
                                                 if (emails.Count > 0)
                                                 {
                                                     string vmta = "mta-" + servers[current][0].Replace(":", ".");
                                                     if (servers[current].Length == 6)
                                                     {
                                                         vmta = $"{mta}-{servers[current][0].Replace(":", ".")}-{servers[current][5]}";
                                                     }
                                                     string rdns = Text.Rdns(servers[current][0], servers[current][1]);
                                                     string rp = Text.Build_rp(raw_rp, servers[current][1], rdns, "info");
                                                     rp = IsPlaceholder ? Placeholder.ReplaceRotate(rp, placeholder_counter, true) : rp;
                                                     Message message = new Message(rp);
                                                     string header = Text.Header_normal(raw_hd);
                                                     string genB = Text.ReplaceBoundary(header + "\n" + raw_bd + "\n\n");
                                                     message.AddMergeData(Text.Generate(genB));
                                                     message.VirtualMTA = vmta;
                                                     message.JobID = Id.ToString();
                                                     message.EnvID = Id.ToString();
                                                     message.Verp = false;
                                                     message.Encoding = Encoding.EightBit;

                                                     foreach (string[] email in emails)
                                                     {
                                                         string currentEmail = IsAutoReply ? Reply.ThreadGetAndRotate() : email[1];                                                        

                                                         Recipient r = new Recipient(currentEmail);
                                                         //links                                           
                                                         string key = Text.Adler32($"{Id}{email[0]}");
                                                         r["red"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-{email[0]}-{key}-{SendId}-" + Text.Random("[rnda/20]"));
                                                         r["unsub"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-{email[0]}-{key}-{SendId}-" + Text.Random("[rnda/20]"));
                                                         r["opn"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-{email[0]}-{key}-{SendId}-" + Text.Random("[rnda/20]"));

                                                         //header body
                                                         r["pe"] = $"b,{Id},{Username},{servers[current][0]},{servers[current][3]},{email[0]}";
                                                         r["ip"] = servers[current][0];
                                                         r["server"] = (string)details_server.name + servers[current][4];
                                                         r["domain"] = servers[current][1];
                                                         r["idi"] = servers[current][2];
                                                         r["idd"] = servers[current][3];
                                                         r["ids"] = servers[current][4];
                                                         r["ide"] = email[0];
                                                         r["rdns"] = rdns;
                                                         r["name"] = currentEmail.Split('@')[0];
                                                         r["to"] = email[1];
                                                         r["reply"] = currentEmail;                                                       
                                                         r["date"] = Text.GetRFC822Date();
                                                         r["boundary"] = Text.Random("[rndlu/30]");
                                                         r["bnd"] = Text.Boundary(header);
                                                         r["*parts"] = "1";
                                                         if (IsPlaceholder)
                                                         {
                                                             r = Placeholder.ReplaceRotateReciption(r, placeholder_counter, true);
                                                         }

                                                         message.AddRecipient(r);
                                                         Interlocked.Increment(ref c_seed);
                                                         Interlocked.Increment(ref placeholder_counter);

                                                         if (Seed != 0 && c_seed % Seed == 0 && seed_emails.Length > 0)
                                                         {
                                                             foreach (string test_email in seed_emails)
                                                             {
                                                                 string currentTest = IsAutoReply ? Reply.GetCurrent() : test_email;

                                                                 Recipient t = new Recipient(currentTest);
                                                                 //links
                                                                 string tkey = Text.Adler32($"{Id}0");
                                                                 t["red"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]"));
                                                                 t["unsub"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]"));
                                                                 t["opn"] = Text.Base64Encode(Text.Random("[rnda/20]") + "-" + $"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]"));

                                                                 //header body
                                                                 t["pe"] = $"t,{Id},{Username},{servers[current][0]},{servers[current][3]},0";
                                                                 t["ip"] = servers[current][0];
                                                                 t["server"] = (string)details_server.name + servers[current][4];
                                                                 t["domain"] = servers[current][1];
                                                                 t["idi"] = servers[current][2];
                                                                 t["idd"] = servers[current][3];
                                                                 t["ids"] = servers[current][4];
                                                                 t["ide"] = "0";
                                                                 t["rdns"] = rdns;
                                                                 t["name"] = currentTest.Split('@')[0];
                                                                 t["to"] = test_email;
                                                                 t["reply"] = currentTest;                                                               
                                                                 t["date"] = Text.GetRFC822Date();
                                                                 t["boundary"] = Text.Random("[rndlu/30]");
                                                                 t["bnd"] = Text.Boundary(header);
                                                                 t["*parts"] = "1";
                                                                 if (IsPlaceholder)
                                                                 {
                                                                     t = Placeholder.ReplaceCurrentReciption(t);
                                                                 }
                                                                 message.AddRecipient(t);
                                                             }
                                                         }
                                                     }                                                     
                                                     p.Send(message);
                                                     p.Close();
                                                 }
                                             }
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         logger.Error($"ERR {ex.Message} -- {ex.StackTrace}");
                                         Console.WriteLine($"ERR {ex.Message} -- {ex.StackTrace}");
                                     }
                                 })
                            );
                        }
                        Task.WaitAll(tasks.ToArray());
                        campaign.Campaign_update_send(Id, toSend + progress);
                    }
                    else
                    {
                        Result.Add("Campaign Not Found " + Id);
                        logger.Debug("Campaign Not Found " + Id);
                        campaign.Campaign_update_progress(Id, "start", true, 0);
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERR {ex.Message} -- {ex.StackTrace}");
                    logger.Error($"ERR {ex.Message} -- {ex.StackTrace}");
                }
                Thread.Sleep(Sleep * 1000);
            }
            campaign.Campaign_update_progress(Id, "start", true, 0);
            return Result;
        }
    }
}
