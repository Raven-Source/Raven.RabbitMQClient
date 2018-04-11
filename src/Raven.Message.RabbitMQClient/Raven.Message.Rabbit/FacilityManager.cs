using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client.Exceptions;

namespace Raven.Message.RabbitMQ
{
    internal class FacilityManager
    {
        private ClientConfiguration _clientConfiguration;
        ILog _log;
        ChannelManager _channelManager;

        Dictionary<string, string> _declaredQueue;
        Dictionary<string, string> _declaredExchange;

        internal FacilityManager(ILog log, ClientConfiguration config, ChannelManager channelManager)
        {
            _clientConfiguration = config;
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
            _declaredQueue = new Dictionary<string, string>(_clientConfiguration.QueueConfigs.Count + 4);
            _declaredExchange = new Dictionary<string, string>(_clientConfiguration.ExchangeConfigurations.Count + 4);
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
                    queueName = queueConfig.Name;
                    durable = queueConfig.Durable;
                    autoDelete = queueConfig.AutoDelete;
                    parms = new Dictionary<string, object>();
                    if (queueConfig.MaxPriority > 0)
                    {
                        parms.Add("x-max-priority", queueConfig.MaxPriority);
                    }
                    if (queueConfig.MaxLength>0)
                    {
                        parms.Add("x-max-length", queueConfig.MaxLength);
                    }
                    if (queueConfig.Expiration>0)
                    {
                        parms.Add("x-message-ttl", queueConfig.Expiration);
                    }
                    if (!string.IsNullOrWhiteSpace(queueConfig.DeadExchange))
                    {
                        parms.Add("x-dead-letter-exchange", queueConfig.DeadExchange);
                    }
                    if (!string.IsNullOrWhiteSpace(queueConfig.DeadMessageKeyPattern))
                    {
                        parms.Add("x-dead-letter-routing-key", queueConfig.DeadMessageKeyPattern);
                    }
                }
                try
                {
                    channel.QueueDeclare(queueName, durable, false, autoDelete, parms);
                    string parmStr = parms == null ? "" : string.Join("; ", parms.Select(p => p.Key + p.Value));
                    _log?.LogDebug($"declare queue {queueName}, durable:{durable}, autoDelete:{autoDelete}, parms:{parmStr}", null);
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
                            _log?.LogError($"DeclareQueue failed, {queueName}", ex, null);
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
                DeclareExchange(ref bindToExchange, channel, _clientConfiguration.ExchangeConfigurations[bindToExchange]);
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
                    exchangeName = exchangeConfig.Name;
                }
                channel.ExchangeDeclare(exchangeName, exchangeType, durable, autoDelete, null);
                _declaredExchange.Add(exchange, exchangeName);
                exchange = exchangeName;
                _log?.LogDebug($"declare exchange {exchangeName}, exchangeType:{exchangeType}", null);
            }
        }

        internal void DeclareBind(IModel channel, string queue, string exchange, string routingKey)
        {
            channel.QueueBind(queue, string.IsNullOrEmpty(exchange) ? "amq.direct" : exchange, routingKey ?? "");
            _log?.LogDebug($"declare bind, queue:{queue}, exchange:{exchange}, routingKey:{routingKey}", null);
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
