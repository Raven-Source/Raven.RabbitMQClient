using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Client.Init();
            Client client = Client.GetInstance("localhost");

            //bool onreceiveSuccess = client.Consumer.OnReceive<string>("testqueue", TestQueueReceived);
            //Console.WriteLine("onreceive success:{0}", onreceiveSuccess);

            //bool subscribeSuccess = client.Consumer.Subscribe<string>("testexchange", "testqueue1", "#", Test1QueueReceived);
            //Console.WriteLine("subscribe success:{0}", subscribeSuccess);

            bool sendSuccess = client.Producer.Send<string>("hello world", "testqueue");
            client.Producer.Send<string>("hello world", "testqueue");
            client.Producer.Send<string>("hello world", "testqueue");
            client.Producer.Send<string>("hello world", "testqueue");
            client.Producer.Send<string>("hello world", "testqueue");
            Console.WriteLine("send success:{0}", sendSuccess);

            bool publishSuccess = client.Producer.Publish<string>("hello world", "testexchange");
            Console.WriteLine("publish success:{0}", publishSuccess);

            Console.WriteLine("type enter to exit");
            Console.ReadLine();
            Client.Dispose();
        }

        public static bool TestQueueReceived(string message, string messageKey, string messageId, string correlationId)
        {
            Console.WriteLine("TestQueueReceived, {0}", message);
            return true;
        }

        public static bool Test1QueueReceived(string message, string messageKey, string messageId, string correlationId)
        {
            Console.WriteLine("Test1QueueReceived, {0}", message);
            return true;
        }
    }
}
