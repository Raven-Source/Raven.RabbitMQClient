using System;
using Raven.Message.RabbitMQ;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;

namespace Raven.EventClient.Rabbit
{
    /// <summary>
    /// 使用rabbitmq作中间件的事件发布系统
    /// </summary>
    internal class RabbitEventClient:IEventClient
    {
        private readonly IRabbitClient _client;
        private readonly IConfigCache _configCache;


        public RabbitEventClient(ClientConfiguration config, IConfigCache configCache,ILog log)
        {
            _client = ClientFactory.Create(config,log);
            _configCache = configCache;
        }

        public bool Publish<T>(int eventId, T data)
        {
            var config = _configCache.GetProducerConfig(eventId);
            if(config==null)
                throw new Exception($"请先配置需要发布的事件!事件ID:{eventId}");
            return _client.Publish(data, config.RouteKey, config.Exchange);
        }


        public bool Subscribe<T>(string configId, Func<T, bool> onRecived)
        {
            var config = _configCache.GetConsumeConfig(configId);
            if (config == null)
                throw new Exception($"请先配置需要监听的事件及业务!ID:{configId}");
            return _client.Subscribe(config.QueueName, config.Exchange, config.RouteKey, onRecived);
        }

        public void Dispose()
        {
            try
            {
                _client.Dispose();
            }
            catch (Exception)
            {
                //
            }
        }
    }
}
