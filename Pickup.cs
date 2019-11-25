using NLog;
using send.helpers;
using Send.helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Send
{
    class Pickup
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

        public Pickup(dynamic data)
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
            Encryption enc = new Encryption(); //links encrpytion
            Campaign campaign = new Campaign(Artisan);
            int c_seed = 0;

            dynamic cdata = campaign.Campaign_info(Id); //get info campaign

            if(cdata != null)
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
                int countIps = servers.Count;
                List<Task> tasks = new List<Task>();

                for (int i = 0; i < Loop; i++)
                {
                    var info_send = campaign.Campaign_send_info(Id);
                    if (info_send != null)
                    {
                        int dataToSend = countIps * Fraction;
                        int total_sended = int.Parse((string)info_send.send_progress);
                        int file_count = int.Parse((string)info_send.send_count);
                        int value_to_send;
                        if (total_sended + dataToSend >= file_count)
                        {
                            if (total_sended - file_count <= 0)
                            {
                                campaign.Campaign_update_progress(Id, "finish", true, 0);
                                Result.Add("Campaign Ended" + Id);
                                return Result;
                            }
                            else
                            {
                                value_to_send = file_count - total_sended;
                            }
                        }
                        else
                        {
                            value_to_send = dataToSend;
                        }


                        List<int> ipLimit = Campaign.DistributeInteger(value_to_send, countIps).ToList();

                        for (int j = 0; j < countIps; j++)
                        {
                            int current = j;

                            tasks.Add(
                                 Task.Factory.StartNew(() =>
                                 {
                                     try
                                     {
                                         if(ipLimit[current] > 0)
                                         {
                                             var details_server = campaign.Server_info(int.Parse(servers[current][4]));
                                             if (details_server != null)
                                             {
                                                 int skip = (current == 0 ? 0 : ipLimit.Take(current).Sum()) + total_sended;
                                                 List<string[]> emails = File.ReadLines(file).Skip(skip).Take(ipLimit[current])
                                                             .Select(t => t.Trim().Split(','))
                                                             .Where(item => item.Length == 2)
                                                             .ToList();
                                                 if (emails.Count > 0)
                                                 {
                                                     PickupFile helper = new PickupFile();
                                                 }
                                             }
                                         }
                                     }
                                     catch(Exception ex)
                                     {
                                         logger.Error($"ERR {ex.Message} -- {ex.StackTrace}");
                                         Console.WriteLine($"ERR {ex.Message} -- {ex.StackTrace}");
                                     }
                                 })
                            );
                        }

                        Task.WaitAll(tasks.ToArray());
                        campaign.Campaign_update_send(Id, value_to_send+total_sended);
                        campaign.Campaign_update_progress(Id, "start", true, 0);

                    }
                    else
                    {
                        Result.Add("Cant get Send progress campaign" + Id);
                        logger.Error("Cant get Send progress campaign" + Id);
                        campaign.Campaign_update_progress(Id, "start", true, 0);
                    }
                }






            }



            return Result;
        }
    }
}
