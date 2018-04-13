using System;
using System.Collections.Concurrent;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// Rabbit客户端工厂
    /// </summary>
    public static  class ClientFactory
    {
        static readonly ConcurrentDictionary<string,IRabbitClient> Clients=new ConcurrentDictionary<string, IRabbitClient>();
        /// <summary>
        /// 创建rabbitmq客户端
        /// </summary>
        /// <param name="config">rabbitmq配置</param>
        /// <param name="log">日志工具</param>
        /// <returns></returns>
        public static IRabbitClient Create(ClientConfiguration config,ILog log=null)
        {
            if (Clients.TryGetValue(config.Name, out var client))
                return client;
            client=new RabbitClient(config,log);
            Clients.TryAdd(config.Name, client);
            return client;
        }

        public static void CloseAll()
        {
            foreach (var client in Clients.Values)
            {
                try
                {
                   client.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
