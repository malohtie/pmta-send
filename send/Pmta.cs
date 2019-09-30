using port25.pmta.api.submitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace send
{
    class Pmta
    {
        private Connection Socket { get; set; }
        public Pmta(string ip, string password, string username = "api", int port = 2525)
        {
            Socket = new Connection(ip, port, username, password);
        }
        public void Send(Message message) => Socket.Submit(message);
        public void Close() => Socket.Close();
    }
}
