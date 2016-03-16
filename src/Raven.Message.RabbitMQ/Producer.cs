using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using Raven.Serializer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 生产者
    /// </summary>
    public class Producer
    {
        internal BrokerConfiguration BrokerConfig { get; set; }

        internal ILog Log { get; set; }

        internal ChannelManager Channel { get; set; }

        internal FacilityManager Facility { get; set; }

        internal Producer()
        {
        }

        BuffProducer _buffProducer = null;

        BuffProducer Buffer
        {
            get
            {
                if (_buffProducer == null)
                {
                    lock (this)
                    {
                        if (_buffProducer == null)
                        {
                            _buffProducer = new Producer.BuffProducer(this);
                        }
                    }
                }
                return _buffProducer;
            }
        }

        /// <summary>
        /// 往指定队列发送消息，消息只会被消费一次
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="queue">队列名</param>
        /// <param name="option">附加参数 <see cref="Raven.Message.RabbitMQ.SendOption"/></param>
        public bool Send<T>(T message, string queue, SendOption option = null)
        {
            return SendInternal(message, queue, option, true);
        }
        /// <summary>
        /// 发送异步方法，往指定队列发送消息，消息只会被消费一次
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="queue">队列名</param>
        /// <param name="option">附加参数 <see cref="Raven.Message.RabbitMQ.SendOption"/></param>
        public void SendToBuff<T>(T message, string queue, SendOption option = null)
        {
            Buffer.SendToBuff(message, queue, option);
        }
        /// <summary>
        /// 发布消息，消息会被每个订阅者消费
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="exchange">路由器名</param>
        /// <param name="messageKey">消息关键字，若关键字有多个请用.分割</param>
        public bool Publish<T>(T message, string exchange, string messageKey = null)
        {
            ExchangeConfiguration exchangeConfig = null;
            if (BrokerConfig.ExchangeConfigs != null)
            {
                exchangeConfig = BrokerConfig.ExchangeConfigs[exchange];
            }
            if (exchangeConfig == null)
            {
                Log.LogError("exchange config not found", null, message);
                return false;
            }
            IModel channel = Channel.GetChannel();
            if (channel == null)
                return false;
            try
            {
                Facility.DeclareExchange(exchange, channel, exchangeConfig);
                byte[] body = SerializeMessage(message, exchangeConfig.SerializerType);
                bool success = DoSend(body, exchange, messageKey, channel);
                if (success)
                {
                    Log.LogDebug(string.Format("publish success, {0} {1}", exchange, messageKey), message);
                    Channel.ReturnChannel(channel);
                }
                else
                {
                    Log.LogError(string.Format("publish failed, {0} {1}", exchange, messageKey), null, message);
                }
                return success;
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("publish failed, {0} {1}", exchange, messageKey), ex, message);
                return false;
            }
        }

        private bool SendInternal<T>(T message, string queue, SendOption option, bool sync)
        {
            QueueConfiguration queueConfig = null;
            if (BrokerConfig.QueueConfigs != null)
                queueConfig = BrokerConfig.QueueConfigs[queue];
            if (queueConfig == null)
            {
                Log.LogError("queue config not found", null, message);
                return false;
            }
            IModel channel = Channel.GetChannel();
            if (channel == null)
                return false;
            try
            {
                Facility.DeclareQueue(queue, channel, queueConfig);
                byte[] body = SerializeMessage(message, queueConfig.SerializerType);
                bool doConfirm = false;
                int confirmTimeout = 0;
                string replyTo = null;
                bool persistent = false;
                byte priority = 0;
                string messageId = null;
                string correlationId = null;
                if (queueConfig.ProducerConfig != null)
                {
                    doConfirm = queueConfig.ProducerConfig.SendConfirm && sync;
                    confirmTimeout = queueConfig.ProducerConfig.SendConfirmTimeout;
                    if (queueConfig.ProducerConfig.MessagePersistent || !string.IsNullOrEmpty(queueConfig.ProducerConfig.ReplyQueue))
                    {
                        replyTo = queueConfig.ProducerConfig.ReplyQueue;
                        persistent = queueConfig.ProducerConfig.MessagePersistent;
                    }
                }
                if (option != null)
                {
                    priority = option.Priority;
                    messageId = option.MessageId;
                    correlationId = option.CorrelationId;
                    if (!string.IsNullOrEmpty(option.ReplyQueue))
                    {
                        replyTo = option.ReplyQueue;
                    }
                }

                bool success = DoSend(body, null, queue, channel, doConfirm, confirmTimeout, replyTo, persistent, priority, messageId, correlationId);
                if (success)
                {
                    Log.LogDebug(string.Format("send success, {0}", queue), message);
                    Channel.ReturnChannel(channel);
                }
                else
                {
                    Log.LogError(string.Format("send failed, {0}", queue), null, message);
                }
                return success;
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("send failed, {0}", queue), ex, message);
                return false;
            }
        }

        private bool DoSend(byte[] message, string exchange, string routingKey, IModel channel, bool doConfirm = false, int confirmTimeout = 0, string replyTo = null, bool persistent = false, byte priority = 0, string messageId = null, string correlationId = null)
        {
            IBasicProperties properties = null;
            if (!string.IsNullOrEmpty(replyTo) || persistent || priority > 0 || !string.IsNullOrEmpty(messageId) || !string.IsNullOrEmpty(correlationId))
            {
                properties = channel.CreateBasicProperties();
                properties.ReplyTo = replyTo;
                properties.Persistent = persistent;
                properties.Priority = priority;
                properties.MessageId = messageId;
                properties.CorrelationId = correlationId;
            }
            if (doConfirm)
            {
                channel.ConfirmSelect();
            }
            channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: properties, body: message);
            bool confirmed = false;
            if (doConfirm)
            {
                confirmed = channel.WaitForConfirms(TimeSpan.FromMilliseconds(confirmTimeout));
                return confirmed;
            }
            return true;
        }

        private byte[] SerializeMessage<T>(T message, SerializerType? serializerType)
        {
            SerializerType sType;
            if (serializerType != null)
                sType = serializerType.Value;
            else
                sType = ClientConfiguration.Instance.SerializerType;
            return SerializerService.Serialize(message, sType);
        }

        class BuffProducer
        {
            private AutoResetEvent _resetEvent;
            private ConcurrentQueue<BuffMessage> _queue;
            private Thread _queueWorkThread;
            private Producer _producer;
            public BuffProducer(Producer producer)
            {
                _producer = producer;
                _queue = new ConcurrentQueue<BuffMessage>();
                _resetEvent = new AutoResetEvent(false);
                _queueWorkThread = new Thread(QueueToWrite);
                _queueWorkThread.IsBackground = true;
                _queueWorkThread.Start();
            }

            internal void SendToBuff<T>(T message, string queue, SendOption option = null)
            {
                BuffMessage buffMessage = new BuffMessage();
                buffMessage.Message = message;
                buffMessage.Queue = queue;
                buffMessage.Option = option;

                _queue.Enqueue(buffMessage);
                _resetEvent.Set();
            }

            private void QueueToWrite()
            {
                while (true)
                {
                    _resetEvent.Reset();
                    if (_queue.Count > 0)
                    {
                        BuffMessage qm = null;
                        while (_queue.TryPeek(out qm))
                        {
                            if (_producer.SendInternal<object>(qm.Message, qm.Queue, qm.Option, false))
                            {
                                _queue.TryDequeue(out qm);
                            }
                            else
                            {
                                break;
                            }
                            SpinWait.SpinUntil(() => false, 1);
                        }
                    }
                    _resetEvent.WaitOne(10000);
                    //SpinWait.SpinUntil(() => false, waitMillisecondsTimeout);
                    //Thread.Sleep(waitMillisecondsTimeout);
                }
            }

            class BuffMessage
            {
                public object Message { get; set; }
                public string Queue { get; set; }

                public SendOption Option { get; set; }
            }
        }
    }
}
