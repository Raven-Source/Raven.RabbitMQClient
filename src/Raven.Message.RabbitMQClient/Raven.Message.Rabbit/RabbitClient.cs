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
            Available = true;
        }
        


        public bool Send<T>(T message, string queue,  SendOption option = null)
        {
            return _producer.Send(message, queue, option);
        }

        public bool Publish<T>(T message, string queue, string exchange)
        {
            return _producer.Publish(message, exchange, queue);
        }

        public bool Subscribe<T>(string queue, string exchange,string routeKey, Func<T, bool> onRecived)
        {
            var recived=new MessageReceived<T>((message, key, id, correlationId, args) => onRecived(message));
            return _consumer.Subscribe(exchange, queue, routeKey, recived);
        }

        public bool Recived<T>(string queue, Func<T, bool> onRecived)
        {
            var recived = new MessageReceived<T>((message, key, id, correlationId, args) => onRecived(message));
            return _consumer.OnReceive(queue, recived);
        }

        public bool Recived<T, TReply>(string queue, Func<T, TReply> onRecived)
        {
            var reply = new MessageReceived<T, TReply>((message, key, id, correlationId, args) => onRecived(message));
            return _consumer.OnReceive(queue, reply);

        }

        public bool Available { get; private set; }

        public void Dispose()
        {
            try
            {
                Available = false;
                _channel.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
