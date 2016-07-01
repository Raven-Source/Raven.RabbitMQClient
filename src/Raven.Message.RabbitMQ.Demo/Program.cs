using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Demo
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("{0} start init", DateTime.Now);
            string testqueue = "test1";
            Client.Init();

            Console.WriteLine("{0} init complete", DateTime.Now);

            Client client = Client.GetInstance("testbroker");



            bool onreceiveSuccess = client.Consumer.OnReceive<string>(testqueue, new Abstract.MessageReceived<string>(TestQueueReceived));
            //onreceiveSuccess = client.Consumer.OnReceive<string>("testqueue", new Abstract.MessageReceived<string>(TestQueueReceived));
            //onreceiveSuccess = client.Consumer.OnReceive<string>("testqueue", new Abstract.MessageReceived<string>(TestQueueReceived));
            //onreceiveSuccess = client.Consumer.OnReceive<string>("testqueue", new Abstract.MessageReceived<string>(TestQueueReceived));
            Console.WriteLine("{1}, onreceive success:{0}", onreceiveSuccess, DateTime.Now);

            //bool subscribeSuccess = client.Consumer.Subscribe<string>("testexchange", "testqueue1", "#", Test1QueueReceived);
            //Console.WriteLine("subscribe success:{0}", subscribeSuccess);

            bool sendSuccess = client.Producer.Send<string>("hello world", testqueue);
            client.Producer.Send<string>("hello world", testqueue);
            //client.Producer.Send<string>("hello world", "testqueue");
            //client.Producer.Send<string>("hello world", "testqueue");
            //client.Producer.Send<string>("hello world", "testqueue");
            Console.WriteLine("{1}, send success:{0}", sendSuccess, DateTime.Now);

            //bool publishSuccess = client.Producer.Publish<string>("hello world", "testexchange");
            //Console.WriteLine("publish success:{0}", publishSuccess);

            Console.WriteLine("type enter to exit");
            Console.ReadLine();
            Client.Dispose();
        }
        static string _log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mq.log");
        public static bool TestQueueReceived(string message, string messageKey, string messageId, string correlationId, BasicDeliverEventArgs args)
        {
            //File.AppendAllText(_log, "message received:" + message);
            //System.Diagnostics.Debug.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " message received");
            Console.WriteLine("{1}, TestQueueReceived, {0}", message, DateTime.Now);
            //Thread.Sleep(300000);
            return true;
        }

        public static bool Test1QueueReceived(string message, string messageKey, string messageId, string correlationId)
        {
            Console.WriteLine("Test1QueueReceived, {0}", message);
            return true;
        }
    }
}
