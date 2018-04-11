using System;
using System.Collections.Generic;
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
            var config = new ClientConfiguration();
            config.Uri = "amqp://127.0.0.1";
            config.Name = "rabbit";
            config.SerializerType = SerializerType.NewtonsoftJson;
            config.QueueConfigs=new Dictionary<string, QueueConfiguration>();
            var producerconfig=new ProducerConfiguration();
            var queue1=new QueueConfiguration("queueA",producerConfig:producerconfig);
            var queue2=new QueueConfiguration("queueB",producerConfig:producerconfig);
            var queue3=new QueueConfiguration("queueC",producerConfig:producerconfig);
            config.QueueConfigs.Add(queue1.Name,queue1);
            config.QueueConfigs.Add(queue2.Name,queue2);
            config.QueueConfigs.Add(queue3.Name,queue3);
            config.ExchangeConfigurations=new Dictionary<string, ExchangeConfiguration>();
            var exchange=new ExchangeConfiguration("exchange1",producerconfig,exchangeType:ExchangeType.Topic);
            config.ExchangeConfigurations.Add(exchange.Name,exchange);
            var client = ClientFactory.Create(config);
            client.Subscribe<string>(queue1.Name,exchange.Name,"*",Recive);
            client.Subscribe<string>(queue1.Name,exchange.Name,"*",Recive);
            client.Subscribe<string>(queue2.Name,exchange.Name,"*",Recive);
            client.Subscribe<string>(queue3.Name,exchange.Name,"*",Recive);
            client.Publish("test message","*",exchange.Name);
            Console.Read();
            ClientFactory.CloseAll();
        }

        static bool Recive(string message)
        {
            Console.WriteLine(message);
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
