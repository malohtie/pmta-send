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
    class NormalM
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
        public Rotation Reply { get; set; }
        public Rotation Placeholder { get; set; }
        public string Negative { get; set; }

        public NormalM(dynamic data)
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
        }

        public List<string> Send()
        {
            List<string> Result = new List<string>();
            Campaign campaign = new Campaign(Artisan);
         
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
                        var servers = Campaign.Convert_ips(Convert.ToString(cdata.ips), Convert.ToString(cdata.option));
                        if(servers.Count == 0)
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
                            Placeholder = new Rotation(cdata.placeholder_data, (int)cdata.placeholder_every);
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

                        foreach (var server in servers)
                        {
                            try
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
                                                string rdns = Text.Rdns(ip["ip"], ip["domain"]);
                                                string rp = Text.Build_rp(raw_rp, ip["domain"], rdns, "info");
                                                Message message = new Message(rp);
                                                string header = Text.Header_normal(raw_hd);
                                                string genB = Text.ReplaceBoundary(header + "\n" + raw_bd + "\n\n");
                                                message.AddMergeData(Text.Generate(genB));
                                                message.VirtualMTA = ip["vmta"];
                                                message.JobID = Id.ToString();
                                                message.EnvID = Id.ToString();
                                                message.Verp = false;
                                                message.Encoding = Encoding.EightBit;

                                                foreach (string[] email in emails)
                                                {
                                                    string currentEmail = IsAutoReply ? Reply.GetAndRotate() : email[1];
                                                    string placeHolder = IsPlaceholder ? Placeholder.GetAndRotate() : "";

                                                    Recipient r = new Recipient(currentEmail);
                                                    //links                                           
                                                    string key = Text.Adler32($"{Id}{email[0]}");
                                                    r["red"] = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");
                                                    r["unsub"] = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");
                                                    r["opn"] = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");

                                                    //header body
                                                    r["pe"] = $"n,{Id},{Username},{ip["ip"]},{ip["idd"]},{email[0]}";
                                                    r["ip"] = ip["ip"];
                                                    r["server"] = (string)details_server.name + ip["ids"];
                                                    r["domain"] = ip["domain"];
                                                    r["idi"] = ip["idi"];
                                                    r["idd"] = ip["idd"];
                                                    r["ids"] = ip["ids"];
                                                    r["ide"] = email[0];
                                                    r["rdns"] = rdns;
                                                    r["name"] = currentEmail.Split('@')[0];
                                                    r["to"] = email[1];
                                                    r["reply"] = currentEmail;
                                                    r["placeholder"] = placeHolder;
                                                    r["date"] = Text.GetRFC822Date();
                                                    r["boundary"] = Text.Random("[rndlu/30]");
                                                    r["bnd"] = Text.Boundary(header);
                                                    r["*parts"] = "1";

                                                    message.AddRecipient(r);

                                                    total_send++;
                                                    c_seed++;

                                                    if (Seed != 0 && c_seed % Seed == 0 && seed_emails.Length > 0)
                                                    {
                                                        foreach (string test_email in seed_emails)
                                                        {
                                                            string currentTest = IsAutoReply ? Reply.GetCurrent() : test_email;
                                                            string placeholderTest = IsPlaceholder ? Placeholder.GetCurrent() : "";

                                                            Recipient t = new Recipient(currentTest);
                                                            //links
                                                            string tkey = Text.Adler32($"{Id}0");
                                                            t["red"] = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");
                                                            t["unsub"] = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");
                                                            t["opn"] = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");

                                                            //header body
                                                            t["pe"] = $"t,{Id},{Username},{ip["ip"]},{ip["idd"]},0";
                                                            t["ip"] = ip["ip"];
                                                            t["server"] = (string)details_server.name + ip["ids"];
                                                            t["domain"] = ip["domain"];
                                                            t["idi"] = ip["idi"];
                                                            t["idd"] = ip["idd"];
                                                            t["ids"] = ip["ids"];
                                                            t["ide"] = "0";
                                                            t["rdns"] = rdns;
                                                            t["name"] = currentTest.Split('@')[0];
                                                            t["to"] = test_email;
                                                            t["reply"] = currentTest;
                                                            t["placeholder"] = placeholderTest;
                                                            t["date"] = Text.GetRFC822Date();
                                                            t["boundary"] = Text.Random("[rndlu/30]");
                                                            t["bnd"] = Text.Boundary(header);
                                                            t["*parts"] = "1";
                                                            message.AddRecipient(t);
                                                        }
                                                    }
                                                }
                                                p.Send(message);
                                                //Task.Run(() => campaign.Campaign_update_send(Id, total_send + total_sended));
                                                campaign.Campaign_update_send(Id, total_send + total_sended);
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
                            catch(Exception ex)
                            {
                                Console.WriteLine($"ERR SERVER ID {server.Key} {ex.Message} -- {ex.StackTrace}");
                                logger.Error($"ERR SERVER ID {server.Key}  {ex.Message} -- {ex.StackTrace}");
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
