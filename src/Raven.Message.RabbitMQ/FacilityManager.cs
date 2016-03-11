using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;

namespace Raven.Message.RabbitMQ
{
    internal class FacilityManager
    {

        BrokerConfiguration _brokerConfig;
        ILog _log;

        List<string> _declaredQueue;

        internal FacilityManager(ILog log, BrokerConfiguration brokerConfig)
        {
            _brokerConfig = brokerConfig;
            _log = log;
            _declaredQueue = new List<string>(brokerConfig.QueueConfigs.Count);
        }

        internal void DeclareQueue(string queue, IModel channel, QueueConfiguration queueConfig)
        {
            if (_declaredQueue.Contains(queue))
                return;
            lock (queue)
            {
                if (_declaredQueue.Contains(queue))
                    return;
                Dictionary<string, object> parms = null;
                if (queueConfig.MaxPriority > 0)
                {
                    if (parms == null)
                        parms = new Dictionary<string, object>();
                    parms.Add("x-max-priority", queueConfig.MaxPriority);
                }
                if (queueConfig.MaxLength > 0)
                {
                    if (parms == null)
                        parms = new Dictionary<string, object>();
                    parms.Add("x-max-length", queueConfig.MaxLength);
                }
                if (queueConfig.Expiration > 0)
                {
                    if (parms == null)
                        parms = new Dictionary<string, object>();
                    parms.Add("x-message-ttl", queueConfig.Expiration);
                }
                try
                {
                    channel.QueueDeclare(queue, queueConfig.Durable, false, queueConfig.AutoDelete, parms);
                }
                catch (OperationInterruptedException)
                {
                    if (queueConfig.RedeclareWhenFailed)
                    {
                        channel.QueueDelete(queue);
                        channel.QueueDeclare(queue, queueConfig.Durable, false, queueConfig.AutoDelete, parms);
                    }
                }

                if (!string.IsNullOrEmpty(queueConfig.BindToExchange))
                {
                    DeclareBind(channel, queue, queueConfig.BindToExchange, queueConfig.BindMessageKey);
                }
                _declaredQueue.Add(queue);
            }
        }

        internal void DeclareExchange(string exchange, IModel channel, ExchangeConfiguration exchangeConfig)
        {
            throw new NotImplementedException();
        }

        internal void DeclareBind(IModel channel, string queue, string exchange, string routingKey)
        {
            try
            {
                channel.QueueBind(queue, exchange, routingKey);
            }
            catch (OperationInterruptedException)
            { }
        }
    }
}
