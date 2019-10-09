using Newtonsoft.Json;
using NLog;
using Org.BouncyCastle.Utilities.Encoders;
using send.helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace send
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        static void log(bool status, string message)
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
            try
            {
                if (args.Length == 2)
                {
                    switch (args[0].ToLower())
                    {
                        case "test":
                            if (Text.IsBase64String(args[1]))
                            {
                                // Begin timing
                                stopwatch.Start();
                                dynamic gdata = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(args[1]));
                                GlobalTest gtest = new GlobalTest(gdata);
                                List<string> gresult = gtest.Send();
                                // Stop timing
                                stopwatch.Stop();
                                Console.Write(string.Join("<br>", gresult)+"<br>TOOK : " + stopwatch.Elapsed.ToString());
                            }
                            else
                            {
                                Console.Write("TEST, INVALID DATA");
                                logger.Warn("TEST, INVALID DATA B64");
                            }
                            break;
                        case "ctest":
                            if (Text.IsBase64String(args[1]))
                            {
                                // Begin timing
                                stopwatch.Start();
                                dynamic data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(args[1]));
                                Ctest test = new Ctest(data);
                                List<string> result = test.Send();
                                // Stop timing
                                stopwatch.Stop();
                                Console.Write(string.Join("<br>", result) + "<br>TOOK : " + stopwatch.Elapsed.ToString());
                            }
                            else
                            {
                                Console.Write("TEST, INVALID DATA");
                                logger.Warn("TEST, INVALID DATA B64");
                            }
                            break;
                        case "delay":
                            break;
                        case "normal":
                            break;
                        default:
                            Console.Write("UNKNOW ACTION");
                            break;

                    }
                }
                else
                {
                    Console.Write("BAD ARGUMENTS");
                    logger.Warn("BAD ARGUMENTS PASSED");
                }
            }
            catch(Exception ex)
            {
                Console.Write("EXEPTION "+ex.Message);
                logger.Error($"{ex.Message}_{ex.StackTrace}");
            }
        }
    }
}
