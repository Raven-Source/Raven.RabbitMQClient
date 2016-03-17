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
        ChannelManager _channelManager;

        List<string> _declaredQueue;
        List<string> _declaredExchange;

        internal FacilityManager(ILog log, BrokerConfiguration brokerConfig, ChannelManager channelManager)
        {
            _brokerConfig = brokerConfig;
            _log = log;
            _channelManager = channelManager;
            _channelManager.ConnectionShutdown += _channelManager_ConnectionShutdown;
            Reset();
        }

        private void _channelManager_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Reset();
        }

        internal void Reset()
        {
            _declaredQueue = new List<string>(_brokerConfig.QueueConfigs.Count);
            _declaredExchange = new List<string>(_brokerConfig.ExchangeConfigs.Count);
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
                bool durable = false;
                bool autoDelete = false;
                if (queueConfig != null)
                {
                    durable = queueConfig.Durable;
                    autoDelete = queueConfig.AutoDelete;
                    if (queueConfig.MaxPriority > 0)
                    {
                        if (parms == null)
                            parms = new Dictionary<string, object>();
                        parms.Add("x-max-priority", queueConfig.MaxPriority);
                    }
                    if (queueConfig.MaxLength != null)
                    {
                        if (parms == null)
                            parms = new Dictionary<string, object>();
                        parms.Add("x-max-length", queueConfig.MaxLength);
                    }
                    if (queueConfig.Expiration != null)
                    {
                        if (parms == null)
                            parms = new Dictionary<string, object>();
                        parms.Add("x-message-ttl", queueConfig.Expiration);
                    }
                }
                try
                {
                    channel.QueueDeclare(queue, durable, false, autoDelete, parms);
                }
                catch (OperationInterruptedException)
                {
                    if (queueConfig != null && queueConfig.RedeclareWhenFailed)
                    {
                        channel.QueueDelete(queue);
                        channel.QueueDeclare(queue, queueConfig.Durable, false, queueConfig.AutoDelete, parms);
                    }
                }

                if (queueConfig != null && !string.IsNullOrEmpty(queueConfig.BindToExchange))
                {
                    DeclareBind(channel, queue, queueConfig.BindToExchange, queueConfig.BindMessageKey);
                }
                _declaredQueue.Add(queue);
            }
        }

        internal void DeclareExchange(string exchange, IModel channel, ExchangeConfiguration exchangeConfig)
        {
            if (_declaredExchange.Contains(exchange))
                return;
            lock (exchange)
            {
                if (_declaredExchange.Contains(exchange))
                    return;
                try
                {
                    string exchangeType = "topic";
                    if (exchangeConfig != null&&!string.IsNullOrEmpty(exchangeConfig.ExchangeType))
                    {
                        exchangeType = exchangeConfig.ExchangeType;
                    }
                    channel.ExchangeDeclare(exchange, exchangeType, true, false, null);
                }
                catch (OperationInterruptedException)
                {
                }
            }
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
