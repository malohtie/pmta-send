using Newtonsoft.Json;
using NLog;
using send;
using send.helpers;
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
                        if (Text.IsBase64String(data))
                        {
                            switch (args[0].ToLower())
                            {
                                case "test":
                                    // Begin timing
                                    dynamic global_data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(data));
                                    GlobalTest global_test = new GlobalTest(global_data);
                                    List<string> global_result = global_test.Send();
                                    // Stop timing
                                    stopwatch.Stop();
                                    Console.Write(string.Join("<br>", global_result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                                    break;
                                case "ctest":
                                    dynamic campaing_test_data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(data));
                                    Ctest campaign_test = new Ctest(campaing_test_data);
                                    List<string> test_result = campaign_test.Send();
                                    // Stop timing
                                    stopwatch.Stop();
                                    Console.Write(string.Join("<br>", test_result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                                    break;
                                case "delay":
                                    dynamic dalay_data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(data));
                                    Xdelay delay_send = new Xdelay(dalay_data);
                                    List<string> delay_result = delay_send.Send();
                                    stopwatch.Stop();
                                    Console.Write(string.Join("<br>", delay_result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                                    break;                              
                                case "normal":
                                    dynamic normal_data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(data));
                                    NormalM normal_send = new NormalM(normal_data);
                                    List<string> normal_result = normal_send.Send();
                                    stopwatch.Stop();
                                    Console.Write(string.Join("<br>", normal_result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                                    Console.ReadLine();
                                    break;
                                //case "bulk":
                                //    dynamic bulk_data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(args[1]));
                                //    BulkM bulk_send = new BulkM(bulk_data);
                                //    List<string> bulk_result = bulk_send.Send();
                                //    stopwatch.Stop();
                                //    Console.Write(string.Join("<br>", bulk_result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                                //    Console.ReadLine();
                                //    break;
                                default:
                                    Console.Write("UNKNOW ACTION");
                                    break;

                            }
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
