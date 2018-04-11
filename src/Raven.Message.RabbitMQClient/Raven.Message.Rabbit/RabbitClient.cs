using System;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;

namespace Raven.Message.RabbitMQ
{
    internal class RabbitClient:IRabbitClient
    {
        private readonly Consumer _consumer;
        private readonly Producer _producer;
        private readonly ChannelManager _channel;
        internal RabbitClient(ClientConfiguration config,ILog log)
        {
             _channel = new ChannelManager(config.Uri,log);
            var facility=new FacilityManager(log,config, _channel);
            _producer =new Producer(_channel, config,log,facility);
            _consumer=new Consumer(_channel, config,log,_producer,facility);
        }
        


        public void Send<T>(T message, string queue,  SendOption option = null)
        {
            _producer.Send(message, queue, option);
        }

        public void Publish<T>(T message, string queue, string exchange)
        {
            _producer.Publish(message, exchange, queue);
        }

        public void Subscribe<T>(string queue, string exchange,string routeKey, Func<T, bool> onRecived)
        {
            var recived=new MessageReceived<T>((message, key, id, correlationId, args) => onRecived(message));
            _consumer.Subscribe(exchange, queue, routeKey, recived);
        }

        public void Recived<T>(string queue, Func<T, bool> onRecived)
        {
            var recived = new MessageReceived<T>((message, key, id, correlationId, args) => onRecived(message));
            _consumer.OnReceive(queue, recived);
        }

        public void Dispose()
        {
            try
            {
                _channel.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
