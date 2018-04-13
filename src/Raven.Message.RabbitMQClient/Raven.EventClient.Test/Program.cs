using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            //client.Subscribe<string>("1_sendmsg", m => Recive(m, "send msg A "));
            //client.Subscribe<string>("1_sendmsg", m => Recive(m, "send msg B "));

            //监听事件集群B 监听事件ID为1，业务ID为completeorder 
             client.Subscribe<string>("1_completeorder", m => Recive(m, "completeorder A "));
            //client.Subscribe<string>("1_completeorder", m => Recive(m, "completeorder B "));


            //Console.WriteLine($"publish order_start");
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //for (int i = 0; i < 10000000; i++)
            //{
            //    //发布事件消息
            //    client.Publish(1, $"order_ {i}");
            //    //Thread.Sleep(10);
            //}
            //sw.Stop();
            //Console.WriteLine($"publish 100000 order_end,useed:{sw.ElapsedMilliseconds}ms");

            Console.Read();
            client.Dispose();
        }
        static int recivecount = 0;
        static bool Recive(string message, string subName)
        {
           // Interlocked.Increment(ref recivecount);
           // Console.WriteLine(subName + ":" + message);
            return true;
        }
    }
}
