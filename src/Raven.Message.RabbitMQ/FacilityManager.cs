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

        internal void DeclareQueue(string queue, ref IModel channel, QueueConfiguration queueConfig)
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
                    string parmStr = parms == null ? "" : string.Join("; ", parms.Select(p => p.Key + p.Value));
                    _log.LogDebug($"declare queue {queue}, durable:{durable}, autoDelete:{autoDelete}, parms:{parmStr}", null);
                }
                catch (OperationInterruptedException)
                {
                    if (queueConfig != null && queueConfig.RedeclareWhenFailed)
                    {
                        channel = _channelManager.GetChannel();
                        channel.QueueDelete(queue);
                        channel.QueueDeclare(queue, queueConfig.Durable, false, queueConfig.AutoDelete, parms);
                    }
                    else
                    {
                        //_log.LogError(string.Format("DeclareQueue failed, {0}", queue), ex, null);
                        throw;
                    }
                }
                _declaredQueue.Add(queue);
            }
        }

        internal void DeclareQueueAndBindExchange(string queue, ref IModel channel, QueueConfiguration queueConfig, string bindToExchange, string bindMessageKeyPattern)
        {
            DeclareQueue(queue, ref channel, queueConfig);
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
                DeclareBind(channel, queue, bindToExchange, bindMessageKeyPattern);
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
                string exchangeType = "topic";
                if (exchangeConfig != null && !string.IsNullOrEmpty(exchangeConfig.ExchangeType))
                {
                    exchangeType = exchangeConfig.ExchangeType;
                }
                channel.ExchangeDeclare(exchange, exchangeType, true, false, null);
                _log.LogDebug($"declare exchange {exchange}, exchangeType:{exchangeType}", null);
            }
        }

        internal void DeclareBind(IModel channel, string queue, string exchange, string routingKey)
        {
            channel.QueueBind(queue, exchange, routingKey);
            _log.LogDebug($"declare bind, queue:{queue}, exchange:{exchange}, routingKey:{routingKey}", null);
        }

        internal bool ExistQueue(string queue, IModel channel)
        {
            if (_declaredQueue.Contains(queue))
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
