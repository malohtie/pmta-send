using port25.pmta.api.submitter;
using System;
using send.helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Text.RegularExpressions;

namespace send
{
    class GlobalTest
    {
        public Message Message { get; set; }
        public Recipient Recipient { get; set; }
        public string Return_path { get; set; }
        public string[] Emails { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Mta { get; set; }
        public string Username { get; set; }
        public dynamic Servers { get; set; }

        public GlobalTest(dynamic data)
        {
            this.Return_path = data.return_path ?? "";
            this.Emails = data.emails ?? throw new ArgumentNullException(nameof(data.emails));
            this.Header = Text.Base64Decode(data.header) ?? throw new ArgumentNullException(nameof(data.header));
            this.Body = Text.Base64Decode(data.body) ?? "";
            this.Mta = data.mta ?? throw new ArgumentNullException(nameof(data.mta));
            this.Username = data.username ?? throw new ArgumentNullException(nameof(data.username));
            this.Servers = data.servers ?? throw new ArgumentNullException(nameof(data.servers));
        }

        public GlobalTest(string return_path, string[] emails, string header, string body, string mta, string username, dynamic servers)
        {
            this.Return_path = return_path ?? "";
            this.Emails = emails ?? throw new ArgumentNullException(nameof(emails));
            this.Header = Text.Base64Decode(header) ?? throw new ArgumentNullException(nameof(header));
            this.Body = Text.Base64Decode(body) ?? "";
            this.Mta = mta ?? throw new ArgumentNullException(nameof(mta));
            this.Username = username ?? throw new ArgumentNullException(nameof(username));
            this.Servers = servers ?? throw new ArgumentNullException(nameof(servers));
        }

        public ArrayList send()
        {
            ArrayList data = new ArrayList();
            
            foreach (dynamic server in Servers)
            {
                try
                {
                    Pmta p = new Pmta(server.mainip, server.password, server.userame, server.port);
                    foreach (dynamic ip in Servers.ips)
                    {
                        string email_ip = ip.ip;
                        string domain = ip.domain;
                        string rdns = Text.rdns(email_ip, domain);
                        string vmta_ip = email_ip.Replace(':', '.');
                        string vmta = Mta == "none" ? $"mta-{vmta_ip}" : (Mta == "vmta" ? $"vmta-{vmta_ip}-{Username}" : $"smtp-{vmta_ip}-{Username}");
                        string job = $"0_GLOBAL-TEST-{Username}";
                        
                        foreach(string email in Emails)
                        {
                            string emailName = email.Split('@')[0];
                            string rp = Text.generate(Return_path.ToLower().Replace("[domain]", domain).Replace("[rdns]", rdns).Replace("[name]", emailName));
                            

                        }

                        // $marks = ['[ip]', '[domain]', '[rdns]', '[to]', '[name]'];
                        //$rp = str_ireplace($marks, $replace, Text::spintax(Text::random($raw_returnpath)));
                        //            $hd = str_ireplace($marks, $replace, Text::spintax(Text::random(base64_decode($raw_header))));
                        //            $bd = str_ireplace($marks, $replace, Text::spintax(Text::random(base64_decode($raw_body))));
                        //            $envoiId = $jobId.'_'.date("Y-m-d_h:i:sa");
                    }
                }
                catch (Exception ex)
                {
                    data.Add(ex.Message);
                }
            }
            return data;
        }




    }
}
