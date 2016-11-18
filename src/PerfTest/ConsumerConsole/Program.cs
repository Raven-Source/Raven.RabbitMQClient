using RabbitMQ.Client.Events;
using Raven.Message.RabbitMQ;
using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsumerConsole
{
    class Program
    {
        static Client _client = null;
        static long _received = 0;

        static long _lastReceived = 0;
        static Dictionary<byte, long> _priorityReceived = new Dictionary<byte, long>(10);

        static string _action = ConfigurationManager.AppSettings["action"];

        static void Main(string[] args)
        {
            Client.Init();
            _client = Client.GetInstance("perftest");
            bool success = false;
            switch (_action)
            {
                case "Receive":
                    success = _client.Consumer.OnReceive<string>("testqueue", new MessageReceived<string>(TestQueueReceived));
                    break;
                case "Subscribe":
                    success = _client.Consumer.Subscribe<string>("perfexchange", "sub" + DateTime.Now.Ticks, "test", new MessageReceived<string>(OnSubReceived));
                    break;
            }
            //bool onreceiveSuccess = _client.Consumer.OnReceive<string>("testqueue", TestQueueReceived);
            //bool onreceiveSuccess = _client.Consumer.Subscribe<string>("perfexchange", "sub" + DateTime.Now.Ticks, "test", OnSubReceived);
            Console.WriteLine("start success:{0}", success);

            while (true)
            {
                Thread.Sleep(10000);
                PrintStats();
            }
        }

        public static bool TestQueueReceived(string message, string messageKey, string messageId, string correlationId, BasicDeliverEventArgs args)
        {
            Interlocked.Add(ref _received, 1);
            return true;
        }

        public static bool PriorityMessageReceived(string message, string messageKey, string messageId, string correlationId, BasicDeliverEventArgs args)
        {
            byte priority = args.BasicProperties.Priority;
            if (!_priorityReceived.ContainsKey(priority))
            {
                lock(_priorityReceived)
                {
                    if (!_priorityReceived.ContainsKey(priority))
                    {
                        _priorityReceived.Add(priority, 0);
                    }
                }
            }
            _priorityReceived[priority]++;
            return TestQueueReceived(message, messageKey, messageId, correlationId, args);
        }

        public static bool OnSubReceived(string message, string messageKey, string messageId, string correlationId, BasicDeliverEventArgs args)
        {
            return TestQueueReceived(message, messageKey, messageId, correlationId, args);
        }

        static void PrintStats()
        {
            long received = _received;
            Console.WriteLine($"{DateTime.Now}, totalReceived : {received}, lastReceived : {received - _lastReceived}");
            _lastReceived = received;
            Dictionary<byte, long> copy = null;
            lock (_priorityReceived)
            {
                copy = new Dictionary<byte, long>(_priorityReceived);
            }
            foreach (byte priority in copy.Keys)
            {
                Console.WriteLine($"{DateTime.Now}, priority {priority} : {copy[priority]}");
            }
        }
    }

    public class Log : ILog
    {
        public void LogDebug(string info, object dataObj)
        {

        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "consumer.log"), string.Format("{0} {1} {2} {3}{4}", DateTime.Now, errorMessage, ex, dataObj, Environment.NewLine));
        }
    }
}
