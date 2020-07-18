using NLog;
using port25.pmta.api.submitter;
using send.helpers;
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
        public int Seed { get; set; }
        public string Mta { get; set; }
        public string Artisan { get; set; }
        public string Storage { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }

        public BulkM(dynamic data)
        {
            Id = int.Parse((string)data.campaign);
            Seed = int.Parse((string)data.seed);
            Mta = Convert.ToString(data.mta) ?? "none";
            Artisan = Convert.ToString(data.artisan) ?? throw new ArgumentNullException(nameof(Artisan));
            Storage = Convert.ToString(data.storage) ?? throw new ArgumentNullException(nameof(Storage));
            Password = Convert.ToString(data.password) ?? throw new ArgumentNullException(nameof(Password));
            Username = Convert.ToString(data.username) ?? throw new ArgumentNullException(nameof(Username));
        }

        public List<string> Send()
        {
            List<string> Result = new List<string>();
            Encryption enc = new Encryption(); //links encrpytion
            Campaign campaign = new Campaign(Artisan);
            int c_seed = 0;


            dynamic cdata = campaign.Campaign_info(Id); //get info campaign

            if (cdata != null)
            {
                string raw_rp = Convert.ToString(cdata.return_path);
                string[] seed_emails = Campaign.Convert_emails(Convert.ToString(cdata.email_test));
                string raw_hd = Text.Base64Decode(Convert.ToString(cdata.header));
                string raw_bd = Text.Base64Decode(Convert.ToString(cdata.body));
                var servers = Campaign.Bulk_split(Convert.ToString(cdata.ips));
                string file = Storage + "/" + Convert.ToString(cdata.send_file);
                string platform = Convert.ToString(cdata.platform);
                string redirect_link = Convert.ToString(cdata.redirect_link);
                string unsubscribe_link = Convert.ToString(cdata.unsubscribe_link);

                int countlines = int.Parse(Convert.ToString(cdata.send_count));
                int countIps = servers.Count;

                List<int> ipLimit = Campaign.DistributeInteger(countlines, countIps).ToList();

                List<Task> tasks = new List<Task>();
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
                                         int skip = current == 0 ? 0 : ipLimit.Take(current).Sum();
                                         Console.WriteLine("skip " + skip);

                                         List<string[]> emails = File.ReadLines(file).Skip(skip).Take(ipLimit[current])
                                                             .Select(t => t.Trim().Split(','))
                                                             .Where(item => item.Length == 2)
                                                             .ToList();

                                         if (emails.Count > 0)
                                         {
                                             string email_ip = servers[current][0];
                                             string domain = servers[current][1];
                                             string rdns = Text.Rdns(email_ip, domain);
                                             string vmta_ip = email_ip.Replace(':', '.');
                                             string vmta = Mta.ToLower() == "none" ? $"mta-{vmta_ip}" : (Mta == "vmta" ? $"vmta-{vmta_ip}-{Username}" : $"smtp-{vmta_ip}-{Username}");
                                             string job = $"{Id}";

                                             string rp = Text.Build_rp(raw_rp, domain, rdns, "reply");
                                             Message message = new Message(rp);
                                             string header = Text.Header_normal(raw_hd);
                                             message.AddMergeData(Text.Generate(header + "\n" + raw_bd));
                                             message.VirtualMTA = vmta;
                                             message.JobID = Id.ToString();
                                             message.Verp = false;
                                             message.Encoding = Encoding.EightBit;

                                             foreach (string[] email in emails)
                                             {
                                                 Recipient r = new Recipient(email[1]);
                                                 //links
                                                 r["red"] = enc.Encrypt($"r!!{Id}!!{servers[current][2]}!!{servers[current][3]}!!{email[0]}!!{redirect_link}!!{platform}");
                                                 r["unsub"] = enc.Encrypt($"u!!{Id}!!{servers[current][2]}!!{servers[current][3]}!!{email[0]}!!{unsubscribe_link}");
                                                 r["opn"] = enc.Encrypt($"o!!{Id}!!{servers[current][2]}!!{servers[current][3]}!!{email[0]}");
                                                 r["out"] = enc.Encrypt($"out!!{new Random().Next(5, 15)}");
                                                 r["short"] = enc.Encrypt(email[0]); //shortlink 
                                                 //header body
                                                 r["pe"] = $"n,{Id},{Username},{servers[current][2]},{servers[current][3]},{email[0]}";
                                                 r["ip"] = email_ip;
                                                 r["domain"] = domain;
                                                 r["rdns"] = rdns;
                                                 r["name"] = email[1].Split('@')[0];
                                                 r["to"] = email[1];
                                                 r["date"] = Text.GetRFC822Date();
                                                 r["boundary"] = Text.Random("[rndlu/30]");
                                                 r["*parts"] = "1";

                                                 message.AddRecipient(r);
                                                 Interlocked.Increment(ref c_seed);

                                                 if (Seed != 0 && c_seed % Seed == 0)
                                                 {
                                                     if (seed_emails.Length > 0)
                                                     {
                                                         foreach (string test_email in seed_emails)
                                                         {
                                                             Recipient t = new Recipient(test_email);
                                                             //links
                                                             t["red"] = enc.Encrypt($"r!!{Id}!!{servers[current][2]}!!{servers[current][3]}!!0!!{redirect_link}!!{platform}");
                                                             t["unsub"] = enc.Encrypt($"u!!{Id}!!{servers[current][2]}!!{servers[current][3]}!!0!!{unsubscribe_link}");
                                                             t["opn"] = enc.Encrypt($"o!!{Id}!!{servers[current][2]}!!{servers[current][3]}!!0");
                                                             t["out"] = enc.Encrypt($"out!!{new Random().Next(5, 10)}");
                                                             t["short"] = enc.Encrypt("0"); //shortlink 
                                                             //header body
                                                             t["pe"] = $"t,{Id},{Username},!!{servers[current][2]}!!{servers[current][3]}!!,0";
                                                             t["ip"] = email_ip;
                                                             t["domain"] = domain;
                                                             t["rdns"] = rdns;
                                                             t["name"] = test_email.Split('@')[0];
                                                             t["to"] = email[1];
                                                             t["date"] = Text.GetRFC822Date();
                                                             t["boundary"] = Text.Random("[rndlu/30]");
                                                             t["*parts"] = "1";
                                                             message.AddRecipient(t);
                                                         }
                                                     }
                                                 }
                                             }
                                             p.Send(message);
                                             p.Close();
                                             //Task.Run(() => campaign.Campaign_update_send(Id, skip + ipLimit[current]));
                                             return;
                                         }
                                         else
                                         {
                                             logger.Error("Emails Empty" + Id);
                                             p.Close();
                                             return;
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
                campaign.Campaign_update_send(Id, countlines);
                campaign.Campaign_update_progress(Id, "start", true, 0);
                return Result;
            }
            else
            {
                Result.Add("Campaign Not Found " + Id);
                logger.Debug("Campaign Not Found " + Id);
                campaign.Campaign_update_progress(Id, "start", true, 0);
                return Result;
            }
        }
    }
}
