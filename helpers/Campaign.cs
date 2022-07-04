using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Send.helpers
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
                string data = null;
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "php",
                        Arguments = $"{Path} {cmd}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        //RedirectStandardError = true,                     
                        CreateNoWindow = false
                    }
                };
                proc.Start();
                data = proc.StandardOutput.ReadToEnd().Trim();
                proc.Close();
                return data;
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

        public string Campaign_negative(string id)
        {
            string data = Exec($"campaign:negative {id}");
            if (!string.IsNullOrWhiteSpace(data) && data != "0")
            {
                try
                {
                    return File.ReadAllText(data);
                }
                catch { }

            }
            return "";
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
        public static object Convert_ips(string ips, string option = "ip")
        {
            if (option == "ip")
            {
                return ips.Trim().Split('\n').Select(t => t.Trim().Split(','))
                .Where(item => item.Length == 5)
                .GroupBy(item => item[4])
                .ToDictionary(i => i.Key, i => i.Select(item => new Dictionary<string, string> {
                    {"ip", item[0] },
                    {"domain", item[1] },
                    {"idi", item[2] },
                    {"idd", item[3] },
                    {"ids", item[4] },
                    {"vmta", "mta-"+item[0].Replace(":", ".")},
                }).ToList());
            }
            else
            {
               return ips.Trim().Split('\n').Select(t => t.Trim().Split(','))
              .Where(item => item.Length >= 6)
              .GroupBy(item => item[4])
              .ToDictionary(i => i.Key, i => i.Select(item => new Dictionary<string, string> {
                    {"ip", item[0] },
                    {"domain", item[1] },
                    {"idi", item[2] },
                    {"idd", item[3] },
                    {"ids", item[4] },
                    {"vmta", $"{option}-{item[0].Replace(":", ".")}-{item[5]}"},
<<<<<<< HEAD
                    {"from", item[6] ?? ""}
=======
                    {"from", item.ElementAtOrDefault(6) ?? ""},
>>>>>>> 62efab8d8eaf4567125b1c9977628c08b340a88b
              }).ToList());
            }

        }
        public static object Bulk_split(string ips)
        {
            return ips.Trim().Split('\n').Select(t => t.Trim().Split(','))
                .Where(item => item.Length >= 5)
                .ToList();
        }
        public int CountLinesLINQ(string path) => File.ReadLines(path).Count();

        public static IEnumerable<int> DistributeInteger(int total, int divider)
        {
            if (divider == 0)
            {
                yield return 0;
            }
            else
            {
                int rest = total % divider;
                double result = total / (double)divider;

                for (int i = 0; i < divider; i++)
                {
                    if (rest-- > 0)
                        yield return (int)Math.Ceiling(result);
                    else
                        yield return (int)Math.Floor(result);
                }

                //int rem; v2
                //int div = Math.DivRem(numerator, denominator, out rem);

                //for (int i = 0; i < denominator; i++)
                //{
                //    yield return i < rem ? div + 1 : div;
                //}
            }
        }
        public static string[] Convert_emails(string emails)
        {
            return emails.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
