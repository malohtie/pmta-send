namespace Send.helpers
{
    using System;
    using System.IO;
    using System.Net.Sockets;

    public class SmtpHelper
    {
        private string Ip { get; }
        private string Username { get; }
        private string Password { get; }
        private int Port { get; }

        private string _smtpReplyLine;
        private TcpClient Client { get; set; }
        private NetworkStream Stream { get; set; }
        private StreamWriter Writer { get; set; }
        private StreamReader Reader { get; set; }
        private int Counter { get; set; }

        public SmtpHelper(string ip, int port = 2525, string username = "api", string password = "")
        {
            Ip = ip;
            Port = port;
            Username = username;
            Password = password;
            Connect();

        }

        private void Connect()
        {
            Client = new TcpClient(Ip, Port);
            Stream = Client.GetStream();
            Writer = new StreamWriter(Stream);
            Writer.AutoFlush = true;
            Reader = new StreamReader(Stream);
            Counter = 0;
            Auth();
        }

        private void SendCommand(string command)
        {
            Writer?.WriteLine(command);
        }

        private string GetCramMd5Response(string username, string password, string challengeB64)
        {
            byte[] data = Convert.FromBase64String(challengeB64);
            Hmac hmac = new Hmac(password);
            hmac.Update(data);
            byte[] array = hmac.Final();
            checked
            {
                byte[] array2 = new byte[username.Length + 1 + array.Length];
                CopyInto(array2, 0, Hmac.GetBytes(username));
                array2[username.Length] = 32;
                CopyInto(array2, username.Length + 1, array);
                return Convert.ToBase64String(array2);
            }
        }

        private static void CopyInto(byte[] to, int toPos, byte[] from)
        {
            checked
            {
                for (int i = 0; i < from.Length; i++)
                {
                    to[toPos + i] = from[i];
                }
            }
        }

        public virtual void CheckReply()
        {
            Writer?.Flush();
            string text;
            do
            {
                text = Reader.ReadLine();
                _smtpReplyLine = text;
            }
            while (text[3] == '-');
        }

        public void Auth()
        {
            SendCommand($"AUTH CRAM-MD5");
            CheckReply();
            CheckReply();

            string challenge = _smtpReplyLine.Substring(4);
            string auth = GetCramMd5Response(Username, Password, challenge);
            SendCommand(auth);
            CheckReply();
            string result = _smtpReplyLine.Substring(4);
            if (!result.Contains("succeeded"))
            {
                Quit();
                throw new Exception("Authentication failed");
            }
        }

        public void Prepare(string returnPath, string email = "")
        {
            SendCommand($"MAIL FROM: <{returnPath}>");
            SendCommand($"RCPT TO: <{email}>");
            SendCommand("DATA");
        }

        public void AddData(string email, string jobId, string envId, string vmta)
        {
            var meta = $"x-envid: {envId}\nx-job: {jobId}\nx-virtual-mta: {vmta}\n";

            SendCommand(meta + email);
            SendCommand(".");

            Counter++;
            if (Counter % 100 == 0)
            {
                Quit();
                Connect();
            }
        }

        public void Quit()
        {
            SendCommand("QUIT");
            Reader?.ReadToEnd(); // we must read to validate send, kind of magic but it works, meh
            Client?.Close();

            Client = null;
            Stream = null;
            Writer = null;
            Reader = null;
        }
    }

}
