using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raven.EventClient.Mongo;
using Raven.EventClient.Rabbit;
using Raven.Message.RabbitMQ.Configuration;

namespace Raven.EventClient.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = EventClientFactory.Default.CreateRabbit(new ClientConfiguration("amqp://127.0.0.1","mc"), new MongoConfigService());

            //监听事件集群A 监听事件ID为1，业务ID为sendmsg
            client.Subscribe<string>("1_sendmsg", m=>Recive(m,"send msg A "));
            client.Subscribe<string>("1_sendmsg", m => Recive(m, "send msg B "));

            //监听事件集群B 监听事件ID为1，业务ID为completeorder 
            client.Subscribe<string>("1_completeorder", m => Recive(m, "completeorder A "));
            client.Subscribe<string>("1_completeorder", m => Recive(m, "completeorder B "));

            for (int i = 0; i < 100000; i++)
            {
                Console.WriteLine($"publish order_{i}");
                //发布事件消息
                client.Publish(1, $"order_ {i}");
                //Thread.Sleep(10);
            }

            Console.Read();
            client.Dispose();
        }
        static bool Recive(string message, string subName)
        {
            Console.WriteLine(subName + ":" + message);
            return true;
        }
    }
}
