using Newtonsoft.Json;
using NLog;
using Send.helpers;
using Send.modes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Send
{
    class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static void Log(bool status, string message)
        {
            Dictionary<string, object> response = new Dictionary<string, object>
            {
                { "status", status },
                { "response", message },
            };
            Console.Write(JsonConvert.SerializeObject(response));
        }

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                if (args.Length == 2)
                {
                    if (File.Exists(args[1]))
                    {
                        string data = File.ReadAllText(args[1]);
                        List<string> result = null;
                        if (Text.IsBase64String(data))
                        {
                            dynamic file_data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(data));
                            switch (args[0].ToLower())
                            {
                                case "test":                                    
                                    GlobalTest global_test = new GlobalTest(file_data);
                                    result = global_test.Send();                                   
                                    break;
                                case "warmup":                                   
                                    Warmup warmup_send = new Warmup(file_data);
                                    result = warmup_send.Send();                                    
                                    break;
                                case "warmupm":
                                    WarmupM warmupm_send = new WarmupM(file_data);
                                    result = warmupm_send.Send();
                                 break;
                                case "ctest":                                   
                                    Ctest campaign_test = new Ctest(file_data);
                                    result = campaign_test.Send();                                 
                                    break;
                                case "delay":
                                    Xdelay delay_send = new Xdelay(file_data);
                                    result = delay_send.Send();                                  
                                    break;
                                case "delay_reply":
                                    XdelayReply xdelay_reply_send = new XdelayReply(file_data);
                                    result = xdelay_reply_send.Send();                                  
                                    break;
                                case "normal":
                                    NormalM normal_send = new NormalM(file_data);
                                    result = normal_send.Send();
                                    break;
                                case "bulk":                                 
                                    BulkM bulk_send = new BulkM(file_data);
                                    result = bulk_send.Send();
                                    break;
                                default:
                                    Console.Write("UNKNOW ACTION");
                                    break;
                            }
                            stopwatch.Stop();
                            Console.Write(string.Join("<br>", result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                        }
                        else
                        {
                            Console.Write("INVALID DATA CONFIG");
                            logger.Warn("TEST, INVALID DATA B64");
                        }
                    }
                    else
                    {
                        Console.Write("DROP SETTINGS NOT FOUND");
                        logger.Warn("DROP SETTINGS NOT FOUND "+ args[1]);
                    }
                }
                else
                {
                    Console.Write("BAD ARGUMENTS");
                    logger.Warn("BAD ARGUMENTS PASSED");
                }
            }
            catch (Exception ex)
            {
                Console.Write("EXEPTION " + ex.Message);
                logger.Error($"{ex.Message}_{ex.StackTrace}");
            }
        }
    }
}
