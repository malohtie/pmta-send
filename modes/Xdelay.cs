﻿using NLog;
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
    class Xdelay
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public int Id { get; set; }
        public int Fraction { get; set; }
        public int Loop { get; set; }
        public int Delay { get; set; }
        public int Sleep { get; set; }
        public int Seed { get; set; }
        public string Mta { get; set; }
        public string Option { get; set; }
        public string Artisan { get; set; }
        public string Storage { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public Rotation Reply { get; set; }
        public Rotation Placeholder { get; set; }

        public Xdelay(dynamic data)
        {
            Id = int.Parse((string)data.campaign);
            Fraction = int.Parse((string)data.fraction);
            Loop = int.Parse((string)data.loop);
            Delay = int.Parse((string)data.delay);
            Sleep = int.Parse((string)data.sleep);
            Seed = int.Parse((string)data.seed);
            Mta = Convert.ToString(data.mta) ?? "none";
            Option = Convert.ToString(data.option) ?? "ip";
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
                        var servers = Campaign.Convert_ips(Convert.ToString(cdata.ips), Option);
                        string file = "/" + Convert.ToString(cdata.send_file);
                        bool IsPlaceholder = Convert.ToString(cdata.is_placeholder) == "1";
                        if(IsPlaceholder)
                        {
                            Placeholder = new Rotation(cdata.placeholder_data, (int)cdata.placeholder_every);
                        }
                        bool IsAutoReply = Convert.ToString(cdata.is_auto_reply) == "1";
                        if(IsAutoReply)
                        {
                            Reply = new Rotation(cdata.auto_reply_data, (int)cdata.auto_reply_every);
                        }

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
                                            string ids = ip["ids"];
                                            string domain = ip["domain"];
                                            string rdns = Text.Rdns(email_ip, domain);
                                            string vmta_ip = email_ip.Replace(':', '.');
                                            string vmta = Mta.ToLower() == "none" ? $"mta-{vmta_ip}" : (Mta == "vmta" ? $"vmta-{vmta_ip}-{Username}" : $"smtp-{vmta_ip}-{Username}");
                                            if (Option == "vmta")
                                            {
                                                vmta = $"mta-{vmta_ip}-"+ ip["cmta"];
                                            }

                                            foreach (string[] email in emails)
                                            {
                                                string currentEmail = IsAutoReply ? Reply.GetAndRotate() : email[1];
                                                string placeHolder = IsPlaceholder ? Placeholder.GetAndRotate() : "";
                                                string key = Text.Adler32($"{Id}{email[0]}");

                                                string redirect = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");
                                                string unsubscribe = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");
                                                string open = Text.Base64Encode($"{Id}-{email[0]}-{key}-{random.Next(1000, 99999)}");

                                                string boundary = Text.Random("[rndlu/30]");
                                                string bnd = Text.boundary(raw_hd);
                                                string hd = Text.replaceBoundary(raw_hd);
                                                string bd = Text.replaceBoundary(raw_bd);
                                                string emailName = email[1].Split('@')[0];
                                                string rp = Text.Build_rp(raw_rp, domain, rdns, emailName, currentEmail, placeHolder);
                                                hd = Text.Build_header(hd, email_ip, ids, domain, rdns, email[1], emailName, boundary, bnd, currentEmail, placeHolder);
                                                hd = Text.Inject_header(hd, "x", Id.ToString(), Username, ip["ip"], ip["idd"], email[0]);
                                                bd = Text.Build_body(bd, email_ip, ids, domain, rdns, email[1], emailName, redirect, unsubscribe, open, boundary, bnd, currentEmail, placeHolder);
                                                Message message = new Message(rp);
                                                message.AddData(Text.replaceBoundary(hd + "\n" + bd + "\n\n", bnd));
                                                message.AddRecipient(new Recipient(currentEmail));
                                                message.VirtualMTA = vmta;
                                                message.JobID = Id.ToString();
                                                message.Verp = false;
                                                message.Encoding = Encoding.EightBit;
                                                p.Send(message);
                                                total_send++;
                                                c_seed++;
                                                Task.Run(() => campaign.Campaign_update_send(Id, total_send + total_sended));
                                                if (Seed != 0 && c_seed % Seed == 0)
                                                {
                                                    Console.WriteLine("Seed : " + c_seed);
                                                    if (seed_emails.Length > 0)
                                                    {
                                                        foreach (string test_email in seed_emails)
                                                        {
                                                            string currentTest = IsAutoReply ? Reply.GetCurrent() : test_email;
                                                            string placeholderTest = IsPlaceholder ? Placeholder.GetCurrent() : "";

                                                            string tkey = Text.Adler32($"{Id}0");
                                                            string tredirect = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");
                                                            string tunsubscribe = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");
                                                            string topen = Text.Base64Encode($"{Id}-0-{tkey}-{random.Next(1000, 99999)}");

                                                            string tboundary = Text.Random("[rndlu/30]");
                                                            string tbnd = Text.boundary(raw_hd);
                                                            string thd = Text.replaceBoundary(raw_hd);
                                                            string tbd = Text.replaceBoundary(raw_bd);
                                                            string temailName = test_email.Split('@')[0];
                                                            string trp = Text.Build_rp(raw_rp, domain, rdns, temailName, currentTest, placeholderTest);
                                                            thd = Text.Build_header(thd, email_ip, ids, domain, rdns, test_email, temailName, tboundary, tbnd, currentTest, placeholderTest);
                                                            thd = Text.Inject_header(thd, "x", Id.ToString(), Username, ip["ip"], ip["idd"]);
                                                            tbd = Text.Build_body(tbd, email_ip, ids, domain, rdns, test_email, temailName, tredirect, tunsubscribe, topen, tboundary, tbnd, currentTest, placeholderTest);                                                           
                                                            Message testMessage = new Message(trp);
                                                            testMessage.AddData(thd + "\n" + tbd + "\n\n");
                                                            testMessage.AddRecipient(new Recipient(currentTest));
                                                            testMessage.VirtualMTA = vmta;
                                                            testMessage.JobID = Id.ToString();
                                                            testMessage.Verp = false;
                                                            testMessage.Encoding = Encoding.EightBit;
                                                            p.Send(testMessage);
                                                        }
                                                    }
                                                }
                                            }


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
                    //Console.WriteLine($"ERR {ex.Message} -- {ex.StackTrace}");
                    logger.Error($"ERR {ex.Message} -- {ex.StackTrace}");
                }
                Thread.Sleep(Sleep * 1000);
            }
            campaign.Campaign_update_progress(Id, "start", true, 0);
            return Result;
        }
    }
}