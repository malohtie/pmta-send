using Newtonsoft.Json;
using NLog;
using Send.helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Send.modes
{
    internal class XdelaySmtp
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

        public XdelaySmtp(dynamic data)
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

        public async Task<List<string>> Send()
        {
            List<string> Result = new List<string>();
            Campaign campaign = new Campaign(Artisan);
            int c_seed = 0;
            int placeholder_counter = 1;
            Random random = new Random();
            Task pendingUpdateTask = null; // Track pending async update task

            // Get campaign info once outside the loop - static configuration data
            dynamic cdata = campaign.Campaign_info(Id);
            if (cdata == null)
            {
                Result.Add("Campaign Not Found " + Id);
                logger.Debug("Campaign Not Found " + Id);
                campaign.Campaign_update_progress(Id, "start", true, 0);
                return Result;
            }

            // Extract all campaign configuration data once
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

            // Get initial send info once outside the loop
            var initial_info_send = campaign.Campaign_send_info(Id);
            if (initial_info_send == null)
            {
                Result.Add("Cant get Send progress campaign" + Id);
                logger.Error("Cant get Send progress campaign" + Id);
                campaign.Campaign_update_progress(Id, "start", true, 0);
                return Result;
            }

            int initial_total_sended = int.Parse((string)initial_info_send.send_progress);
            int file_count = int.Parse((string)initial_info_send.send_count);
            int cumulative_sent = 0; // Track total sent across all loops

            // Read all config then loop, fixed content
            for (int i = 0; i < Loop; i++) //loop
            {
                Console.WriteLine("Loop : " + i);
                int loop_total_send = 0; // Track total sent in this loop iteration
                int current_total_sended = initial_total_sended + cumulative_sent; // Current position in file

                // Wait for any pending update task from previous loop iteration if not ended
                if (pendingUpdateTask != null && i < Loop - 1) // Don't wait on the last iteration
                {
                    try
                    {
                        await pendingUpdateTask;
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Error waiting for pending update task: {ex.Message}");
                    }
                    pendingUpdateTask = null;
                }

                try
                {
                    foreach (var server in servers)
                    {
                        try
                        {
                            var details_server = campaign.Server_info(int.Parse((string)server.Key));
                            if (details_server != null)
                            {
                                SmtpHelper smtp = new SmtpHelper((string)details_server.ip, password: Password); //load smtp
                                foreach (var ip in server.Value)
                                {
                                    string account = "";
                                    try
                                    {
                                        account = (string)ip["from"] ?? "";
                                        Console.WriteLine("Account : " + account);
                                    }
                                    catch
                                    {
                                        account = "";
                                        Console.WriteLine("err : " + account);
                                    }

                                    // Each IP sends exactly Fraction emails (or remaining emails if less than Fraction)
                                    int total_send = 0, value_to_send = 0;
                                    int ip_total_sended = current_total_sended + loop_total_send; // Current position for this IP
                                    
                                    // Calculate how many emails this IP should send
                                    if (ip_total_sended + Fraction >= file_count)
                                    {
                                        if (file_count - ip_total_sended <= 0)
                                        {
                                            // No more emails to send, campaign ended
                                            // Wait for any pending update before finishing
                                            if (pendingUpdateTask != null)
                                            {
                                                try
                                                {
                                                    await pendingUpdateTask;
                                                }
                                                catch (Exception ex)
                                                {
                                                    logger.Error($"Error waiting for final update task: {ex.Message}");
                                                }
                                            }
                                            
                                            campaign.Campaign_update_progress(Id, "finish", true, 0);
                                            Result.Add("Campaign Ended" + Id);
                                            smtp.Quit();
                                            return Result;
                                        }
                                        else
                                        {
                                            value_to_send = file_count - ip_total_sended;
                                        }
                                    }
                                    else
                                    {
                                        value_to_send = Fraction;
                                    }

                                    //load emails

                                    List<string[]> emails = File.ReadLines(Storage + file).Skip(ip_total_sended).Take(value_to_send)
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

                                            string redirect = Text.Random("[rnda/20]") + "-" + $"{Id}-{email[0]}-{key}-{SendId}-" + Text.Random("[rnda/20]");
                                            string unsubscribe = Text.Random("[rnda/20]") + "-" + $"{Id}-{email[0]}-{key}-{SendId}-" + Text.Random("[rnda/20]");
                                            string open = Text.Random("[rnda/20]") + "-" + $"{Id}-{email[0]}-{key}-{SendId}-" + Text.Random("[rnda/20]");

                                            string boundary = Text.Random("[rndlu/30]");
                                            string bnd = Text.Boundary(raw_hd);
                                            string hd = Text.ReplaceBoundary(raw_hd);
                                            string bd = Text.ReplaceBoundary(raw_bd);
                                            string emailName = email[1].Split('@')[0];
                                            string rp = Text.Build_rp(raw_rp, ip["domain"], rdns, emailName, currentEmail, ip["idi"], ip["idd"], ip["ids"], (string)details_server.name + ip["ids"], email[1], account);
                                            rp = IsPlaceholder ? Placeholder.ReplaceRotate(rp, placeholder_counter) : rp; // replace and rotate return path
                                            hd = Text.Build_header(hd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, email[1], emailName, boundary, bnd, currentEmail, ip["idi"], ip["idd"], ip["ids"], email[0], account);
                                            hd = IsPlaceholder ? Placeholder.ReplaceRotate(hd, placeholder_counter) : hd; //replace and rotate header
                                            hd = Text.Inject_header(hd, "x", Id.ToString(), Username, ip["ip"], ip["idd"], email[0]);
                                            bd = Text.Build_body(bd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, email[1], emailName, redirect, unsubscribe, open, boundary, bnd, currentEmail, ip["idi"], ip["idd"], ip["ids"], email[1], account);
                                            bd = IsPlaceholder ? Placeholder.ReplaceRotate(bd, placeholder_counter) : bd; //replace and rotate body


                                            smtp.Prepare(rp, currentEmail);
                                            smtp.AddData(Text.ReplaceBoundary(hd + "\n" + bd + "\n\n", bnd), Id.ToString(), Id.ToString(), ip["vmta"]);

                                            total_send++;
                                            c_seed++;

                                            if (Seed != 0 && c_seed % Seed == 0 && seed_emails.Length > 0)
                                            {
                                                foreach (string test_email in seed_emails)
                                                {
                                                    string currentTest = IsAutoReply ? Reply.GetCurrent() : test_email;

                                                    string tkey = Text.Adler32($"{Id}0");
                                                    string tredirect = Text.Random("[rnda/20]") + "-" + $"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]");
                                                    string tunsubscribe = Text.Random("[rnda/20]") + "-" + $"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]");
                                                    string topen = Text.Random("[rnda/20]") + "-" + $"{Id}-0-{tkey}-{SendId}-" + Text.Random("[rnda/20]");

                                                    string tboundary = Text.Random("[rndlu/30]");
                                                    string tbnd = Text.Boundary(raw_hd);
                                                    string thd = Text.ReplaceBoundary(raw_hd);
                                                    string tbd = Text.ReplaceBoundary(raw_bd);
                                                    string temailName = test_email.Split('@')[0];
                                                    string trp = Text.Build_rp(raw_rp, ip["domain"], rdns, temailName, currentTest, ip["idi"], ip["idd"], ip["ids"], (string)details_server.name + ip["ids"], test_email, account);
                                                    trp = IsPlaceholder ? Placeholder.ReplaceCurrent(trp) : trp; //return path placeholder
                                                    thd = Text.Build_header(thd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, test_email, temailName, tboundary, tbnd, currentTest, ip["idi"], ip["idd"], ip["ids"], "0", account);
                                                    thd = IsPlaceholder ? Placeholder.ReplaceCurrent(thd) : thd; //head placeholder
                                                    thd = Text.Inject_header(thd, "x", Id.ToString(), Username, ip["ip"], ip["idd"]);
                                                    tbd = Text.Build_body(tbd, ip["ip"], (string)details_server.name + ip["ids"], ip["domain"], rdns, test_email, temailName, tredirect, tunsubscribe, topen, tboundary, tbnd, currentTest, ip["idi"], ip["idd"], ip["ids"], "0", account);
                                                    tbd = IsPlaceholder ? Placeholder.ReplaceCurrent(tbd) : tbd; // body placeholder

                                                    smtp.Prepare(trp, currentTest);
                                                    smtp.AddData(Text.ReplaceBoundary(thd + "\n" + tbd + "\n\n", bnd), Id.ToString(), Id.ToString(), ip["vmta"]);
                                                }
                                            }
                                            if (IsPlaceholder) placeholder_counter++; //increment placeholder counter
                                        }

                                        loop_total_send += total_send; // Accumulate total sent in this loop iteration
                                    }
                                    else if (loop_total_send == 0)
                                    {
                                        // Only show "Emails Empty" if we haven't processed any emails yet in this loop
                                        Result.Add("Emails Empty" + Id);
                                        logger.Error("Emails Empty" + Id);
                                        campaign.Campaign_update_progress(Id, "start", true, 0);
                                        smtp.Quit();
                                        return Result;
                                    }
                                    Thread.Sleep(Delay); //sleep delay
                                }

                                smtp.Quit();
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
                catch (Exception ex)
                {
                    //Console.WriteLine($"ERR {ex.Message} -- {ex.StackTrace}");
                    logger.Error($"ERR {ex.Message} -- {ex.StackTrace}");
                }
                finally
                {
                    // Update send progress asynchronously in finally block to ensure it happens even if exceptions occur
                    if (loop_total_send > 0)
                    {
                        cumulative_sent += loop_total_send; // Add to cumulative total
                        int finalCount = initial_total_sended + cumulative_sent;
                        
                        // Start async update task
                        pendingUpdateTask = Task.Run(() =>
                        {
                            try
                            {
                                bool result = campaign.Campaign_update_send(Id, finalCount);
                                if (!result)
                                {
                                    logger.Warn($"Campaign_update_send returned false for campaign {Id}, count {finalCount}");
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error($"Error in Campaign_update_send: {ex.Message} -- {ex.StackTrace}");
                            }
                        });
                    }
                }
                Thread.Sleep(Sleep);
            }
            
            // Wait for final pending update task before finishing
            if (pendingUpdateTask != null)
            {
                try
                {
                    await pendingUpdateTask;
                }
                catch (Exception ex)
                {
                    logger.Error($"Error waiting for final update task: {ex.Message}");
                }
            }
            
            campaign.Campaign_update_progress(Id, "start", true, 0);
            return Result;
        }
    }
}
