using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace send.helpers
{
    class Campaign
    {
        public string Path { get; set; }

        public Campaign(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        private string Exec(string cmd)
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "php",
                        Arguments = $"{Path} {cmd}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false
                    }
                };
                proc.Start();
                proc.WaitForExit();
                return proc.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public dynamic Campaign_info(int id)
        {
            string data = Exec($"campaign:details {id}");
            if (!string.IsNullOrWhiteSpace(data) && data != "null")
            {
                return JObject.Parse(data);
            }

            return null;
        }

        public dynamic Campaign_send_info(int id)
        {
            string data = Exec($"campaign:send {id}");
            if (!string.IsNullOrWhiteSpace(data) && data != "null")
            {
                return JObject.Parse(data);
            }

            return null;
        }

        public dynamic Server_info(int id)
        {
            string data = Exec($"server:details {id}");
            if (!string.IsNullOrWhiteSpace(data) && data != "null")
            {
                return JObject.Parse(data);
            }

            return null;
        }
        public bool Campaign_update_progress(int id, string newProgress, bool usePid = false, int pid = 0)
        {
            string data;
            if (usePid)
            {
                data = Exec($"campaign:progress {id} {newProgress} {pid}");
            }
            else
            {
                data = Exec($"campaign:progress {id} {newProgress}");
            }
            if (!string.IsNullOrWhiteSpace(data) && data != "null")
            {
                return data == "1";
            }

            return false;
        }
        public bool Campaign_update_send(int id, int countSend)
        {
            string data = Exec($"campaign:update {id} {countSend}");
            if (!string.IsNullOrWhiteSpace(data) && data != "null")
            {
                return data == "1";
            }

            return false;
        }
        public static object Convert_ips(string ips)
        {
            return ips.Trim().Split('\n').Select(t => t.Trim().Split(','))
                .Where(item => item.Length == 5)
                .GroupBy(item => item[4])
                .ToDictionary(i => i.Key, i => i.Select(item => new Dictionary<string, string> {
                    {"ip", item[0] },
                    {"domain", item[1] },
                    {"idi", item[2] },
                    {"idd", item[3] },
                }).ToList());
        }

        public static string[] Convert_emails(string emails)
        {
            return emails.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
