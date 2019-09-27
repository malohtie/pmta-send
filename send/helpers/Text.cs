using System;
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

        private static string random(string text)
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
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
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



    }
}
