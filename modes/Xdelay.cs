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
        public string Artisan { get; set; }
        public string Storage { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string SendId { get; set; }
        public Rotation Reply { get; set; }
        public Placeholder Placeholder { get; set; }
        public string Negative { get; set; }

        public Xdelay(dynamic data)
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
            int c_seed = 0;
            int placeholder_counter = 1;
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
                        if (servers.Count == 0)
                        {
                            Result.Add("No Servers To Process");
                            logger.Error("No Servers To Process");
                            campaign.Campaign_update_progress(Id, "start", true, 0);
                            return Result;
                        }
                        string file = "/" + Convert.ToString(cdata.send_file);
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

                        foreach (var server in servers)
                        {
                            try
                            {
                                var details_server = campaign.Server_info(int.Parse((string)server.Key));
                                if (details_server != null)
                                {
                                    Pmta p = new Pmta((string)details_server.ip, Password); //load pmta
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
                                                foreach (string[] email in emails)
                                                {
                                                    string currentEmail = IsAutoReply ? Reply.GetAndRotate() : email[1];                                                    
                                                    string key = Text.Adler32($"{Id}{email[0]}");

                                                    string redirect = Text.Base64Encode($"{Id}-{email[0]}-{key}-{SendId}-{random.Next(1000, 999999)}");
                                                    string unsubscribe = Text.Base64Encode($"{Id}-{email[0]}-{key}-{SendId}-{random.Next(1000, 999999)}");
                                                    string open = Text.Base64Encode($"{Id}-{email[0]}-{key}-{SendId}-{random.Next(1000, 999999)}");

                                                    string boundary = Text.Random("[rndlu/30]");
                                                    string bnd = Text.Boundary(raw_hd);
                                                    string hd = Text.ReplaceBoundary(raw_hd);
                                                    string bd = Text.ReplaceBoundary(raw_bd);
                                                    string emailName = email[1].Split('@')[0];
                                                    string rp = Text.Build_rp(raw_rp, ip["domain"], rdns, emailName, currentEmail, ip["idi"], ip["idd"], ip["ids"], (string)details_server.name + ip["ids"], email[1]);
                                                    rp = IsPlaceholder ? Placeholder.ReplaceRotate(rp, placeholder_counter) : rp; // replace and rotate return path
                                                    hd = Text.Build_header(hd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, email[1], emailName, boundary, bnd, currentEmail, ip["idi"], ip["idd"], ip["ids"], email[0]);
                                                    hd = IsPlaceholder ? Placeholder.ReplaceRotate(hd, placeholder_counter) : hd; //replace and rotate header
                                                    hd = Text.Inject_header(hd, "x", Id.ToString(), Username, ip["ip"], ip["idd"], email[0]);
                                                    bd = Text.Build_body(bd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, email[1], emailName, redirect, unsubscribe, open, boundary, bnd, currentEmail, ip["idi"], ip["idd"], ip["ids"], email[1]);
                                                    bd = IsPlaceholder ? Placeholder.ReplaceRotate(bd, placeholder_counter) : bd; //replace and rotate body

                                                    Message message = new Message(rp);
                                                    message.AddData(Text.ReplaceBoundary(hd + "\n" + bd + "\n\n", bnd));
                                                    message.AddRecipient(new Recipient(currentEmail));
                                                    message.VirtualMTA = ip["vmta"];
                                                    message.JobID = Id.ToString();
                                                    message.EnvID = Id.ToString();
                                                    message.Verp = false;
                                                    message.Encoding = Encoding.EightBit;
                                                    p.Send(message);
                                                    total_send++;
                                                    c_seed++;
                                                    //Task.Run(() => campaign.Campaign_update_send(Id, total_send + total_sended));
                                                    campaign.Campaign_update_send(Id, total_send + total_sended);
                                                    if (Seed != 0 && c_seed % Seed == 0 && seed_emails.Length > 0)
                                                    {
                                                        foreach (string test_email in seed_emails)
                                                        {
                                                            string currentTest = IsAutoReply ? Reply.GetCurrent() : test_email;
                                                         
                                                            string tkey = Text.Adler32($"{Id}0");
                                                            string tredirect = Text.Base64Encode($"{Id}-0-{tkey}-{SendId}-{random.Next(1000, 999999)}");
                                                            string tunsubscribe = Text.Base64Encode($"{Id}-0-{tkey}-{SendId}-{random.Next(1000, 999999)}");
                                                            string topen = Text.Base64Encode($"{Id}-0-{tkey}-{SendId}-{random.Next(1000, 999999)}");

                                                            string tboundary = Text.Random("[rndlu/30]");
                                                            string tbnd = Text.Boundary(raw_hd);
                                                            string thd = Text.ReplaceBoundary(raw_hd);
                                                            string tbd = Text.ReplaceBoundary(raw_bd);
                                                            string temailName = test_email.Split('@')[0];
                                                            string trp = Text.Build_rp(raw_rp, ip["domain"], rdns, temailName, currentTest, "", ip["idi"], ip["idd"], ip["ids"], (string)details_server.name + ip["ids"]);
                                                            trp = IsPlaceholder ? Placeholder.ReplaceCurrent(trp) : trp; //return path placeholder
                                                            thd = Text.Build_header(thd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, test_email, temailName, tboundary, tbnd, currentTest, "", ip["idi"], ip["idd"], ip["ids"]);
                                                            thd = IsPlaceholder ? Placeholder.ReplaceCurrent(thd) : thd; //head placeholder
                                                            thd = Text.Inject_header(thd, "x", Id.ToString(), Username, ip["ip"], ip["idd"]);
                                                            tbd = Text.Build_body(tbd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, test_email, temailName, tredirect, tunsubscribe, topen, tboundary, tbnd, currentTest, "", ip["idi"], ip["idd"], ip["ids"]);
                                                            tbd = IsPlaceholder ? Placeholder.ReplaceCurrent(tbd) : tbd; // body placeholder
                                                            Message testMessage = new Message(trp);
                                                            testMessage.AddData(thd + "\n" + tbd + "\n\n");
                                                            testMessage.AddRecipient(new Recipient(currentTest));
                                                            testMessage.VirtualMTA = ip["vmta"];
                                                            testMessage.JobID = Id.ToString();
                                                            testMessage.EnvID = Id.ToString();
                                                            testMessage.Verp = false;
                                                            testMessage.Encoding = Encoding.EightBit;
                                                            p.Send(testMessage);
                                                        }
                                                    }
                                                    if (IsPlaceholder) placeholder_counter ++; //increment placeholder counter
                                                    
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
                            catch (Exception ex)
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