using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace send.helpers
{
    class Text
    {
        public static string generate(string text)
        {
            return random(spintax(base64(text)));
        }
        public static string rdns(string ip, string domain)
        {
            try
            {

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"dig +short -x {ip}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false
                    }
                };

                proc.Start();
                proc.WaitForExit();
                string rdns = proc.StandardOutput.ReadToEnd().Trim().TrimEnd('.');
                return !string.IsNullOrEmpty(rdns) && rdns != domain ? rdns : domain;

            }
            catch (Exception)
            {
                return domain;
            }
        }
        public static string Build_rp(string return_path, string domain, string rdns, string emailName)
        {
            return_path = Regex.Replace(return_path, @"\[domain\]", domain, RegexOptions.IgnoreCase);
            return_path = Regex.Replace(return_path, @"\[rdns\]", rdns, RegexOptions.IgnoreCase);
            return_path = Regex.Replace(return_path, @"\[name\]", emailName, RegexOptions.IgnoreCase);
            return generate(return_path);
        }
        public static string Build_header(string header, string ip, string domain, string rdns, string email, string emailName, string boundary = null)
        {
            Dictionary<string, string> header_array = new Dictionary<string, string>();

            string[] header_params = header.Split('\n');
            foreach (string param in header_params)
            {
                string[] keys = param.Split(':');
                if (keys.Length == 2)
                {
                    header_array.Add(keys[0], keys[1]);
                }
            }

            string header_result = string.Join("\n", header_array.Select(x => $"{x.Key}:{x.Value}"));
            header_result = Regex.Replace(header_result, @"\[ip\]", ip, RegexOptions.IgnoreCase);
            header_result = Regex.Replace(header_result, @"\[domain\]", domain, RegexOptions.IgnoreCase);
            header_result = Regex.Replace(header_result, @"\[rdns\]", rdns, RegexOptions.IgnoreCase);
            header_result = Regex.Replace(header_result, @"\[name\]", emailName, RegexOptions.IgnoreCase);
            header_result = Regex.Replace(header_result, @"\[to\]", email, RegexOptions.IgnoreCase);
            header_result = Regex.Replace(header_result, @"\[date\]", GetRFC822Date(), RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(boundary))
            {
                header_result = Regex.Replace(header_result, @"\[boundary\]", boundary, RegexOptions.IgnoreCase);
            }
            return generate(header_result);
        }
        public static string Build_body(string body, string ip, string domain, string rdns, string email, string emailName, string boundary = null)
        {
            body = Regex.Replace(body, @"\[ip\]", ip, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[domain\]", domain, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[rdns\]", rdns, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[name\]", emailName, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[to\]", email, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[date\]", GetRFC822Date(), RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(boundary))
            {
                body = Regex.Replace(body, @"\[boundary\]", boundary, RegexOptions.IgnoreCase);
            }
            return generate(body);
        }
        private static string RandomString(int length, int option = 0)
        {
            Random rnd = new Random();

            const string nb = "0123456789";
            const string lw = "abcdefghijklmnopqrstuvwxyz";
            const string up = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (option == 1)   //lower + upper
            {
                return new string(Enumerable.Repeat(up + lw, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
            }
            else if (option == 2)  //upper + numbers
            {
                return new string(Enumerable.Repeat(up + nb, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
            }
            else if (option == 3)  //lower + numbers
            {
                return new string(Enumerable.Repeat(lw + nb, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
            }
            else if (option == 4) //numbers
            {
                return new string(Enumerable.Repeat(nb, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
            }
            else if (option == 5) //lower
            {
                return new string(Enumerable.Repeat(lw, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
            }
            else if (option == 6) //upper
            {
                return new string(Enumerable.Repeat(up, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
            }

            //lower + numbers + upper
            return new string(Enumerable.Repeat(up + lw + nb, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
        }
        public static string random(string text)
        {
            return Regex.Replace(text, @"\[(rnd.*?)\]", delegate (Match match)
            {
                
                string[] data = match.Groups[1].Value.Split('/');

                if(data.Length == 1 && data[0].Equals("rnd"))
                {
                    return RandomString(18);
                }
                else if(data.Length == 2 && int.TryParse(data[1], out int n))
                {
                    if (data[0].Equals("rndn")) //number
                    {
                        return RandomString(int.Parse(data[1]), 4);
                    }
                    else if (data[0].Equals("rnda")) //all
                    {
                        return RandomString(int.Parse(data[1]));
                    }
                    else if (data[0].Equals("rndl")) //lower
                    {
                        return RandomString(int.Parse(data[1]), 5);
                    }
                    else if (data[0].Equals("rndu")) //upper
                    {
                        return RandomString(int.Parse(data[1]), 5);
                    }
                    else if (data[0].Equals("rndul") || data[0].Equals("rndlu")) //upper lower
                    {
                        return RandomString(int.Parse(data[1]), 1);
                    }
                    else if (data[0].Equals("rndln") || data[0].Equals("rndnl")) //number lower
                    {
                        return RandomString(int.Parse(data[1]), 3);
                    }
                    else if (data[0].Equals("rndun") || data[0].Equals("rndnu")) //number upper
                    {
                        return RandomString(int.Parse(data[1]), 2);
                    }    
                }
                return match.ToString();

            }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        }
        private static string base64(string text)
        {
            return Regex.Replace(text, @"\[base64:([^\]]*)\]", delegate (Match match)
            {
                string data = match.Groups[1].Value.ToString() ?? "";
                return Base64Encode(data);

            }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }
        private static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }
        public static string Base64Decode(string base64EncodedData)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedData));
        }
        public static bool IsBase64String(string s)
        {
            return (s.Length % 4 == 0) && Regex.IsMatch(s.Trim(), @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }
        private static string spintax(string text)
        {
            return Regex.Replace(text, @"\[text:([^\]]*)\]", delegate (Match match)
            {
                Random rnd = new Random();
                string[] words = match.Groups[1].Value.ToString().Split('|');
                words = words.Where(y => !string.IsNullOrWhiteSpace(y)).ToArray();
                if(words.Length > 0)
                {
                    return words[rnd.Next(words.Length)];
                }
                return match.Value.ToString();

            }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }
        public static string GetRFC822Date()
        {
            int offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
            string timeZone = "+" + offset.ToString().PadLeft(2, '0');
            if (offset < 0)
            {
                int i = offset * -1;
                timeZone = "-" + i.ToString().PadLeft(2, '0');
            }
            return DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'));

        }
        public static string Generate_links(string body, string url, string unsub, string open, string optout, string shortlink = null)
        {
            body = Regex.Replace(body, @"\[red\]", url, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[unsub\]", unsub, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[out\]", optout, RegexOptions.IgnoreCase);
            body = Regex.Replace(body, @"\[opn\]", open, RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(shortlink))
            {
                body = Regex.Replace(body, @"\[short\]", shortlink, RegexOptions.IgnoreCase);
            }
            return body;
        }
        public static string Header_normal(string header)
        {
            Dictionary<string, string> header_array = new Dictionary<string, string>();

            string[] header_params = header.Split('\n');
            foreach (string param in header_params)
            {
                string[] keys = param.Split(':');
                if (keys.Length == 2)
                {
                    header_array.Add(keys[0], keys[1]);
                }
            }
            header_array.Add("pe", " [pe]");

            return string.Join("\n", header_array.Select(x => $"{x.Key}:{x.Value}"));
        }
        public static string Inject_header(string header, string type, string idc, string idu, string idi, string idd, string ide = "0")
        {
            header = $"pe: {type},{idc},{idu},{idi},{idd},{ide}\n{header}";
            return header;
        }
    }
}
