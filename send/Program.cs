using Newtonsoft.Json;
using NLog;
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
            string[] output;
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
                                dynamic data = JsonConvert.DeserializeObject<dynamic>(Text.Base64Decode(args[1]));
                                GlobalTest test = new GlobalTest(data);
                                var result = test.send();
                                Console.WriteLine(data);
                                Console.WriteLine(data.length);
                                // Stop timing
                                stopwatch.Stop();
                                log(true, "DONE TOOK : "+ stopwatch.Elapsed.ToString());

                            }
                            else
                            {
                                log(false, "TEST, INVALID DATA");
                                logger.Warn("TEST, INVALID DATA B64");
                            }

                            break;

                        default:
                            log(false, "Unknow Action");
                            break;

                    }
                }
                else
                {
                    log(false, "BAD ARGUMENTS");
                    logger.Warn("BAD ARGUMENTS PASSED");
                }
            }
            catch(Exception ex)
            {
                log(false, "EXEPTION "+ex.Message);
                logger.Error(ex.Message);


            }

            

 
            Console.ReadLine();
            //    Console.WriteLine("start "+DateTime.Now.ToString());
            //    String mailfrom = "me.here@some.example-domain.com";
            //    String recipient = mailfrom;

            //    Message msg = new Message(mailfrom);
            //    msg.AddDateHeader();
            //    String headers =
            //        "From: Me Here <" + mailfrom + ">\n" +
            //        "To: Joe Doe <" + recipient + ">\n" +
            //        "Subject: C# HTML test email\n" +
            //        "Content-Type: text/html; charset=\"utf-8\"\n" +
            //        "\n"; // separate headers from body by an empty line
            //    msg.AddData(headers);

            //    String body =
            //        "<html>" +
            //        "<body>" +
            //        "<p>Dear <b>Joe</b>,</p>" +
            //        "<p>This is sent using <a href='http://www.port25.com'>PowerMTA</a>'s" +
            //        "new .NET API!</p>" +
            //        "</body>" +
            //        "</html>";
            //    msg.AddData(body);

            //    Recipient rcpt = new Recipient(recipient);
            //    msg.AddRecipient(rcpt);


            //    Connection con = new Connection("85.93.30.118", 2025, "api", "test");
            //    for (int i = 0; i < 10000; i++)
            //    {
            //        con.Submit(msg);
            //    }

            //    con.Close();
            //    Console.WriteLine("END " + DateTime.Now.ToString());
            //Console.ReadLine();
        }
    }
}
