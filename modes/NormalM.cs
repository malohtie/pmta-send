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
    class NormalM
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public int Id { get; set; }
        public int Fraction { get; set; }
        public int Loop { get; set; }
        public int Delay { get; set; }
        public int Sleep { get; set; }
        public int Seed { get; set; }
        public string Mta { get; set; }
        public string Artisan { get; set; }
        public string Storage { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }

        public NormalM(dynamic data)
        {
            Id = int.Parse((string)data.campaign);
            Fraction = int.Parse((string)data.fraction);
            Loop = int.Parse((string)data.loop);
            Delay = int.Parse((string)data.delay);
            Sleep = int.Parse((string)data.sleep);
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
            Campaign campaign = new Campaign(Artisan);
            Message message;
            int c_seed = 0;
            Random random = new Random();

            for (int i = 0; i < Loop; i++) //loop
            {
                Console.WriteLine("Loop : " + i);
                try
                {
                    dynamic cdata = campaign.Campaign_info(Id);
                    if (cdata != null)
                    {
                        string raw_rp = Convert.ToString(cdata.return_path) ?? "";
                        string[] seed_emails = Campaign.Convert_emails(Convert.ToString(cdata.email_test));
                        string raw_hd = Text.Base64Decode(Convert.ToString(cdata.header));
                        string raw_bd = Text.Base64Decode(Convert.ToString(cdata.body));
                        var servers = Campaign.Convert_ips(Convert.ToString(cdata.ips));
                        string file = "/" + Convert.ToString(cdata.send_file);
                        string platform = Convert.ToString(cdata.platform);
                        string redirect_link = Convert.ToString(cdata.redirect_link);
                        string unsubscribe_link = Convert.ToString(cdata.unsubscribe_link);

                        foreach (var server in servers)
                        {
                            var details_server = campaign.Server_info(int.Parse((string)server.Key));
                            if (details_server != null)
                            {
                                Pmta p = new Pmta(Convert.ToString(details_server.ip), Password); //load pmta
                                foreach (var ip in server.Value)
                                {
                                    var info_send = campaign.Campaign_send_info(Id);
                                    if (info_send != null)
                                    {
                                        int total_send = 0, value_to_send = 0; //check fraction 
                                        int total_sended = int.Parse((string)info_send.send_progress);
                                        int file_count = int.Parse((string)info_send.send_count);
                                        if (total_sended + Fraction >= file_count)
                                        {
                                            if (file_count - total_sended <= 0)
                                            {
                                                campaign.Campaign_update_progress(Id, "finish", true, 0);
                                                Result.Add("Campaign Ended" + Id);
                                                p.Close(); //close
                                                return Result;
                                            }
                                            else
                                            {
                                                value_to_send = file_count - total_sended;
                                            }
                                        }
                                        else
                                        {
                                            value_to_send = Fraction;
                                        }

                                        //load emails

                                        List<string[]> emails = File.ReadLines(Storage + file).Skip(total_sended).Take(value_to_send)
                                            .Select(t => t.Trim().Split(','))
                                            .Where(item => item.Length == 2)
                                            .ToList();
                                        if (emails.Count > 0)
                                        {
                                            string email_ip = ip["ip"];
                                            string domain = ip["domain"];
                                            string rdns = Text.Rdns(email_ip, domain);
                                            string vmta_ip = email_ip.Replace(':', '.');
                                            string vmta = Mta.ToLower() == "none" ? $"mta-{vmta_ip}" : (Mta == "vmta" ? $"vmta-{vmta_ip}-{Username}" : $"smtp-{vmta_ip}-{Username}");
                                            string job = $"{Id}";

                                            string rp = Text.Build_rp(raw_rp, domain, rdns, "reply");
                                            message = new Message(rp);
                                            string header = Text.Header_normal(raw_hd);
                                            string genB = Text.replaceBoundary(header + "\n" + raw_bd + "\n\n");
                                            message.AddMergeData(Text.Generate(genB)); 
                                            message.VirtualMTA = vmta;
                                            message.JobID = Id.ToString();
                                            message.Verp = false;
                                            message.Encoding = Encoding.EightBit;

                                            foreach (string[] email in emails)
                                            {
                                                Recipient r = new Recipient(email[1]);
                                                //links                                           
                                                string key = Text.Adler32($"{Id}{email[0]}");
                                                r["red"] = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");
                                                r["unsub"] = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");
                                                r["opn"] = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");

                                                //header body
                                                r["pe"] = $"n,{Id},{Username},{ip["ip"]},{ip["idd"]},{email[0]}";
                                                r["ip"] = email_ip;
                                                r["domain"] = domain;
                                                r["rdns"] = rdns;
                                                r["name"] = email[1].Split('@')[0];
                                                r["to"] = email[1];
                                                r["date"] = Text.GetRFC822Date();
                                                r["boundary"] = Text.Random("[rndlu/30]");
                                                r["bnd"] = Text.boundary(header);
                                                r["*parts"] = "1";

                                                message.AddRecipient(r);

                                                total_send++;
                                                c_seed++;

                                                if (Seed != 0 && c_seed % Seed == 0)
                                                {
                                                    if (seed_emails.Length > 0)
                                                    {
                                                        foreach (string test_email in seed_emails)
                                                        {
                                                            Recipient t = new Recipient(test_email);
                                                            //links
                                                            string tkey = Text.Adler32($"{Id}0");
                                                            t["red"] = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");
                                                            t["unsub"] = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");
                                                            t["opn"] = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");

                                                            //header body
                                                            t["pe"] = $"t,{Id},{Username},{ip["ip"]},{ip["idd"]},0";
                                                            t["ip"] = email_ip;
                                                            t["domain"] = domain;
                                                            t["rdns"] = rdns;
                                                            t["name"] = test_email.Split('@')[0];
                                                            t["to"] = email[1];
                                                            t["date"] = Text.GetRFC822Date();
                                                            t["boundary"] = Text.Random("[rndlu/30]");
                                                            t["bnd"] = Text.boundary(header);
                                                            t["*parts"] = "1";
                                                            message.AddRecipient(t);
                                                        }
                                                    }
                                                }
                                            }
                                            p.Send(message);
                                            Task.Run(() => campaign.Campaign_update_send(Id, total_send + total_sended));
                                        }
                                        else
                                        {
                                            Result.Add("Emails Empty" + Id);
                                            logger.Error("Emails Empty" + Id);
                                            campaign.Campaign_update_progress(Id, "start", true, 0);
                                            p.Close();
                                            return Result;
                                        }
                                    }
                                    else
                                    {
                                        Result.Add("Cant get Send progress campaign" + Id);
                                        logger.Error("Cant get Send progress campaign" + Id);
                                        campaign.Campaign_update_progress(Id, "start", true, 0);
                                        p.Close();
                                        return Result;
                                    }
                                    Thread.Sleep(Delay * 1000); //sleep delay
                                }
                                p.Close(); //close pmta connection
                            }
                            else
                            {
                                Result.Add("Server Not Found " + server.Key);
                                logger.Error("Server Not Found " + server.Key);
                                campaign.Campaign_update_progress(Id, "start", true, 0);
                                return Result;
                            }
                        }
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
