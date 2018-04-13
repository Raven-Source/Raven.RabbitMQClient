using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using Raven.Serializer;

namespace Raven.Message.RabbitMQ.FrameworkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ClientConfiguration
            {
                Uri = "amqp://127.0.0.1",
                Name = "rabbit",
                SerializerType = SerializerType.NewtonsoftJson
            };
            //config.QueueConfigs=new Dictionary<string, QueueConfiguration>();
            //var producerconfig=new ProducerConfiguration();
            //var queue1=new QueueConfiguration("queueA",producerConfig:producerconfig);
            //var queue2=new QueueConfiguration("queueB",producerConfig:producerconfig);
            //var queue3=new QueueConfiguration("queueC",producerConfig:producerconfig);
            //var queue4=new QueueConfiguration("queueD",producerConfig:producerconfig);
            //config.QueueConfigs.Add(queue1.Name,queue1);
            //config.QueueConfigs.Add(queue2.Name,queue2);
            //config.QueueConfigs.Add(queue3.Name,queue3);
            //config.QueueConfigs.Add(queue4.Name, queue4);
            //config.ExchangeConfigurations=new Dictionary<string, ExchangeConfiguration>();
            //var exchange=new ExchangeConfiguration("exchange1",producerconfig,exchangeType:ExchangeType.Topic);
            //config.ExchangeConfigurations.Add(exchange.Name,exchange);
            var client = ClientFactory.Create(config);
            client.Subscribe<string>("queueA", "exchange1", "first", (msg) => Recive(msg, "first A"));
            client.Subscribe<string>("queueA", "exchange1", "first", (msg) => Recive(msg, "second A"));
            client.Subscribe<string>("queueB", "exchange1", "first", (msg) => Recive(msg, "first B"));
            client.Subscribe<string>("queueC", "exchange1", "first", (msg) => Recive(msg, "first C"));
            client.Recived<string>("queueA", msg => Recive(msg, "one"));
            Console.WriteLine(DateTime.Now);
            for (int i = 0; i < 10; i++)
            {
                client.Publish("test message" + i, "first", "exchange1");
                //client.Send("test message" + i, "queueD");
            }
            Console.Read();
            ClientFactory.CloseAll();
        }
        


        static bool Recive(string message,string subName)
        {
            Console.WriteLine(DateTime.Now+subName+":"+message);
            return true;
        }
    }

    class Log : ILog
    {
        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            Console.WriteLine(errorMessage+":"+ex);
        }

        public void LogDebug(string info, object dataObj)
        {
            Console.WriteLine(info);
        }
    }
}
