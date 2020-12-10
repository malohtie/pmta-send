using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace send.helpers
{
    class Text
    {
        public static string Generate(string text)
        {
            return Base64(Printable(Random(Spintax(text))));
        }
        public static string Rdns(string ip, string domain)
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
            return Generate(return_path);
        }
        public static string Build_header(string header, string ip, string domain, string rdns, string email, string emailName, string boundary = null, string bnd = null)
        {
            string header_result = header;
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
            if (!string.IsNullOrWhiteSpace(bnd))
            {
                header_result = Regex.Replace(header_result, @"\[bnd\]", bnd, RegexOptions.IgnoreCase);
            }
            return Generate(header_result);
        }
        public static string Build_body(string body, string ip, string domain, string rdns, string email, string emailName, string url = null, string unsub = null, string open = null, string boundary = null, string bnd = null)
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
            if (!string.IsNullOrWhiteSpace(bnd))
            {
                body = Regex.Replace(body, @"\[bnd\]", bnd, RegexOptions.IgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                body = Regex.Replace(body, @"\[red\]", url, RegexOptions.IgnoreCase);
            }
            if (!string.IsNullOrWhiteSpace(unsub))
            {
                body = Regex.Replace(body, @"\[unsub\]", unsub, RegexOptions.IgnoreCase);
            }
            if (!string.IsNullOrWhiteSpace(open))
            {
                body = Regex.Replace(body, @"\[opn\]", open, RegexOptions.IgnoreCase);
            }           
            return Generate(body);
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
        public static string Random(string text)
        {
            return Regex.Replace(text, @"(?:\[|\{)(rnd.*?)(?:\]|\})", delegate (Match match)
            {

                string[] data = match.Groups[1].Value.Split('/');

                if (data.Length == 1 && data[0].Equals("rnd"))
                {
                    return RandomString(18);
                }
                else if (data.Length == 2 && int.TryParse(data[1], out int n))
                {
                    if (data[0].Equals("rndn", StringComparison.OrdinalIgnoreCase)) //number
                    {
                        return RandomString(int.Parse(data[1]), 4);
                    }
                    else if (data[0].Equals("rnda", StringComparison.OrdinalIgnoreCase)) //all
                    {
                        return RandomString(int.Parse(data[1]));
                    }
                    else if (data[0].Equals("rndl", StringComparison.OrdinalIgnoreCase)) //lower
                    {
                        return RandomString(int.Parse(data[1]), 5);
                    }
                    else if (data[0].Equals("rndu", StringComparison.OrdinalIgnoreCase)) //upper
                    {
                        return RandomString(int.Parse(data[1]), 5);
                    }
                    else if (data[0].Equals("rndul", StringComparison.OrdinalIgnoreCase) || data[0].Equals("rndlu", StringComparison.OrdinalIgnoreCase)) //upper lower
                    {
                        return RandomString(int.Parse(data[1]), 1);
                    }
                    else if (data[0].Equals("rndln", StringComparison.OrdinalIgnoreCase) || data[0].Equals("rndnl", StringComparison.OrdinalIgnoreCase)) //number lower
                    {
                        return RandomString(int.Parse(data[1]), 3);
                    }
                    else if (data[0].Equals("rndun", StringComparison.OrdinalIgnoreCase) || data[0].Equals("rndnu", StringComparison.OrdinalIgnoreCase)) //number upper
                    {
                        return RandomString(int.Parse(data[1]), 2);
                    }
                }
                return match.ToString();

            }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        }
        public static string boundary(string text)
        {
            Match match = Regex.Match(text, @"\[bnd:([^\]]*)\]", RegexOptions.IgnoreCase);
            if(match.Success)
            {
                return Random(match.Groups[1].Value.ToString() ?? "");
            }
            return "";
        }

        public static string replaceBoundary(string text, string value = null)
        {
            return Regex.Replace(text, @"\[bnd:([^\]]*)\]", delegate (Match match)
            {
                return value ?? "[bnd]";

            }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }
        public static string Base64Encode(string plainText)
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
        private static string Spintax(string text)
        {
            return Regex.Replace(text, @"\[text:([^\]]*)\]", delegate (Match match)
            {
                Random rnd = new Random();
                string[] words = match.Groups[1].Value.ToString().Split('|');
                words = words.Where(y => !string.IsNullOrWhiteSpace(y)).ToArray();
                if (words.Length > 0)
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

        public static string Header_normal(string header)
        {
            return "pe: [pe]\n" + header.Trim();
        }
        public static string Inject_header(string header, string type, string idc, string idu, string idi, string idd, string ide = "0")
        {
            header = $"pe: {type},{idc},{idu},{idi},{idd},{ide}\n{header}";
            return header;
        }
        public static string Adler32(string str)
        {
            const int mod = 65521;
            uint a = 1, b = 0;
            foreach (char c in str)
            {
                a = (a + c) % mod;
                b = (b + a) % mod;
            }
            uint result = (b << 16) | a;
            return result.ToString("x8");
        }
        private static string Printable(string text)
        {
            return Regex.Replace(text, @"\[printable:(.*)\]", delegate (Match match)
            {
                string data = match.Groups[1].Value.ToString() ?? "";
                return PrintableEncode(data);

            }, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        }
        public static string PrintableEncode(string encode, int foldWidth = 76)
        {
            // Don't bother if there's nothing to encode
            if (encode == null || encode.Length == 0)
                return encode;

            StringBuilder sb = new StringBuilder(encode.Length + 100);
            foldWidth--;    // Account for soft line break character
            for (int idx = 0, len = 0; idx < encode.Length; idx++)
            {
                // Characters 9, 32-60, and 62-126 go through as-is as do any Unicode characters
                if (encode[idx] == '\t' || (encode[idx] > '\x1F' && encode[idx] < '=') || (encode[idx] > '=' && encode[idx] < '\x7F') || (int)encode[idx] > 255)
                {
                    if (foldWidth > 0 && len + 1 > foldWidth)
                    {
                        sb.Append("=\r\n");     // Soft line break
                        len = 0;
                    }
                    sb.Append(encode[idx]);
                    len++;
                }
                else
                {
                    // All others encode as =XX where XX is the 2 digit hex value of the character
                    if (foldWidth > 0 && len + 3 > foldWidth)
                    {
                        sb.Append("=\r\n");     // Soft line break
                        len = 0;
                    }
                    sb.AppendFormat("={0:X2}", (int)encode[idx]);
                    len += 3;
                }
            }
            return sb.ToString();
        }
        private static string Base64(string text)
        {
            return Regex.Replace(text, @"\[base64:(.*)\]", delegate (Match match)
            {
                string data = match.Groups[1].Value.ToString() ?? "";
                return Base64EncodeMail(data);

            }, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        }
        public static string Base64EncodeMail(string encode, int foldWidth = 76)
        {
            // Don't bother if there is nothing to encode
            if (encode == null || encode.Length == 0)
                return encode;

            Encoding enc = Encoding.GetEncoding("iso-8859-1");
            byte[] ba = enc.GetBytes(encode);

            StringBuilder sb = new StringBuilder(Convert.ToBase64String(ba));

            // Insert line folds where necessary if requested.
            if (foldWidth > 0)
            {
                for (int idx = foldWidth - 1; idx < sb.Length; idx += foldWidth + 2)
                    sb.Insert(idx, "\r\n");
            }
            return sb.ToString();
        }
    }
}
