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

        Dictionary<string, string> _declaredQueue;
        Dictionary<string, string> _declaredExchange;
        Dictionary<string, string> _declaredBind;

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
            _declaredQueue = new Dictionary<string, string>(_brokerConfig.QueueConfigs.Count + 4);
            _declaredExchange = new Dictionary<string, string>(_brokerConfig.ExchangeConfigs.Count + 4);
            _declaredBind = new Dictionary<string, string>();
        }

        internal void DeclareQueue(ref string queue, ref IModel channel, QueueConfiguration queueConfig, bool throwException)
        {
            if (_declaredQueue.ContainsKey(queue))
            {
                queue = _declaredQueue[queue];
                return;
            }
            lock (queue + "lock")
            {
                if (_declaredQueue.ContainsKey(queue))
                {
                    queue = _declaredQueue[queue];
                    return;
                }
                string queueName = queue;
                Dictionary<string, object> parms = null;
                bool durable = false;
                bool autoDelete = false;
                if (queueConfig != null)
                {
                    queueName = queueConfig.StorageName;
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
                        parms.Add("x-max-length", (int)queueConfig.MaxLength.Value);
                    }
                    if (queueConfig.Expiration != null)
                    {
                        if (parms == null)
                            parms = new Dictionary<string, object>();
                        parms.Add("x-message-ttl", (int)queueConfig.Expiration.Value);
                    }
                    if (queueConfig.DeadExchangeInternal != null)
                    {
                        if (parms == null)
                            parms = new Dictionary<string, object>();
                        parms.Add("x-dead-letter-exchange", queueConfig.DeadExchangeInternal);
                    }
                    if (queueConfig.DeadMessageKeyPatternInternal != null)
                    {
                        if (parms == null)
                            parms = new Dictionary<string, object>();
                        parms.Add("x-dead-letter-routing-key", queueConfig.DeadMessageKeyPatternInternal);
                    }
                }
                try
                {
                    channel.QueueDeclare(queueName, durable, false, autoDelete, parms);
                    string parmStr = parms == null ? "" : string.Join("; ", parms.Select(p => p.Key + p.Value));
                    _log.LogDebug($"declare queue {queueName}, durable:{durable}, autoDelete:{autoDelete}, parms:{parmStr}", null);
                }
                catch (OperationInterruptedException ex)
                {
                    if (queueConfig != null && queueConfig.RedeclareWhenFailed)
                    {
                        channel = _channelManager.GetChannel();
                        channel.QueueDelete(queueName);
                        channel.QueueDeclare(queueName, queueConfig.Durable, false, queueConfig.AutoDelete, parms);
                    }
                    else
                    {
                        if (throwException)
                        {
                            _log.LogError(string.Format("DeclareQueue failed, {0}", queueName), ex, null);
                            throw;
                        }
                        else
                        {
                            channel = _channelManager.GetChannel();
                        }
                    }
                }
                _declaredQueue.Add(queue, queueName);
                queue = queueName;
            }
        }

        internal void DeclareQueueAndBindExchange(ref string queue, ref IModel channel, QueueConfiguration queueConfig, ref string bindToExchange, string bindMessageKeyPattern)
        {
            DeclareQueue(ref queue, ref channel, queueConfig, true);
            if (string.IsNullOrEmpty(bindToExchange) && !string.IsNullOrEmpty(queueConfig?.BindToExchange))
            {
                bindToExchange = queueConfig.BindToExchange;
            }
            if (string.IsNullOrEmpty(bindMessageKeyPattern) && !string.IsNullOrEmpty(queueConfig?.BindMessageKeyPattern))
            {
                bindMessageKeyPattern = queueConfig.BindMessageKeyPattern;
            }
            if (!string.IsNullOrEmpty(bindToExchange))
            {
                DeclareExchange(ref bindToExchange, channel, _brokerConfig.ExchangeConfigs[bindToExchange]);
                DeclareBind(channel, queue, bindToExchange, bindMessageKeyPattern);
            }
            else if (!string.IsNullOrEmpty(bindMessageKeyPattern))
            {
                DeclareQueue(ref bindMessageKeyPattern, ref channel, null, true);
                DeclareBind(channel, queue, bindToExchange, bindMessageKeyPattern);
            }
        }

        internal void DeclareExchange(ref string exchange, IModel channel, ExchangeConfiguration exchangeConfig)
        {
            if (_declaredExchange.ContainsKey(exchange))
            {
                exchange = _declaredExchange[exchange];
                return;
            }
            lock (exchange + "lock")
            {
                if (_declaredExchange.ContainsKey(exchange))
                {
                    exchange = _declaredExchange[exchange];
                    return;
                }
                string exchangeName = exchange;
                string exchangeType = "topic";
                bool durable = false;
                bool autoDelete = false;
                if (exchangeConfig != null)
                {
                    if (!string.IsNullOrEmpty(exchangeConfig.ExchangeType))
                    {
                        exchangeType = exchangeConfig.ExchangeType;
                    }
                    durable = exchangeConfig.Durable;
                    autoDelete = exchangeConfig.AutoDelete;
                    exchangeName = exchangeConfig.StorageName;
                }
                channel.ExchangeDeclare(exchangeName, exchangeType, durable, autoDelete, null);
                _declaredExchange.Add(exchange, exchangeName);
                exchange = exchangeName;
                _log.LogDebug($"declare exchange {exchangeName}, exchangeType:{exchangeType}", null);
            }
        }

        internal void DeclareBind(IModel channel, string queue, string exchange, string routingKey)
        {
            if (string.IsNullOrEmpty(exchange))
            {
                exchange = "amq.direct";
            }
            if (string.IsNullOrEmpty(routingKey))
            {
                routingKey = "";
            }
            string bindId = string.Format("{0}_{1}_{2}", queue, exchange, routingKey);
            if (_declaredBind.ContainsKey(bindId))
            {
                return;
            }
            lock (bindId)
            {
                if (_declaredBind.ContainsKey(bindId))
                    return;
                channel.QueueBind(queue, exchange, routingKey);
                _declaredBind.Add(bindId, bindId);
                _log.LogDebug($"declare bind, queue:{queue}, exchange:{exchange}, routingKey:{routingKey}", null);
            }

        }

        internal bool ExistQueue(string queue, IModel channel)
        {
            if (_declaredQueue.ContainsKey(queue))
            {
                return true;
            }
            bool exist = false;
            try
            {
                channel.QueueDeclarePassive(queue);
                exist = true;
            }
            catch (OperationInterruptedException ex)
            {
                exist = ex.ShutdownReason.ReplyCode != 404;
            }
            return exist;
        }
    }
}
