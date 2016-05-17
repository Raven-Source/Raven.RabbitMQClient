using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    public class RuntimeEnviroment
    {
        public static string IP = GetLocalIp();

        public static string ProcessId = GetProcessId();

        static string GetLocalIp()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            IPAddress localaddr = localhost.AddressList.First((ip) => ip.AddressFamily == AddressFamily.InterNetwork);
            return localaddr.ToString();
        }

        static string GetProcessId()
        {
            return Process.GetCurrentProcess().Id.ToString();
        }
    }
}
