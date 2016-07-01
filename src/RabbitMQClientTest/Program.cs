using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionFactory factory = new ConnectionFactory();
            /*
             * amqp://user:pass@hostName:port/vhost
             */
            factory.Uri = "amqp://test:test@127.0.0.1";

            using (IConnection con = factory.CreateConnection())
            {
                using (IModel channel = con.CreateModel())
                {
                    string queue = channel.QueueDeclare();
                    channel.BasicPublish("", queue, null, Encoding.UTF8.GetBytes("test message"));
                }
            }
        }
    }
}
