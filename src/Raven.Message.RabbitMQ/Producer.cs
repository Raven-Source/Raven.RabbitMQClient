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
        const int ProducerType_Sender = 0;
        const int ProducerType_Publisher = 1;
        internal BrokerConfiguration BrokerConfig { get; set; }

        internal ILog Log { get; set; }

        internal ChannelManager Channel { get; set; }

        internal FacilityManager Facility { get; set; }

        internal Producer()
        {
        }

        Dictionary<string, BuffProducer> _buffProducerDict = null;

        /// <summary>
        /// 往指定队列发送消息，消息只会被消费一次
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="queue">队列名</param>
        /// <param name="option">附加参数 <see cref="Raven.Message.RabbitMQ.SendOption"/></param>
        public bool Send<T>(T message, string queue, SendOption option = null)
        {
            if (string.IsNullOrEmpty(queue))
            {
                Log.LogError("Send queue is null", null, message);
                return false;
            }
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
            if (string.IsNullOrEmpty(queue))
            {
                Log.LogError("SendToBuff queue is null", null, message);
                return;
            }
            BuffProducer producer = GetBuffProducer(queue, ProducerType_Sender);
            producer.SendToBuff(message, queue, option);
        }
        /// <summary>
        /// 发布消息，消息会被每个订阅者消费
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="exchange">路由器名</param>
        /// <param name="messageKey">消息关键字，若关键字有多个请用.分割</param>
        public bool Publish<T>(T message, string exchange, string messageKey = "")
        {
            if (string.IsNullOrEmpty(exchange))
            {
                Log.LogError("Publish exchange is null", null, message);
                return false;
            }
            return PublishInternal<T>(message, exchange, messageKey, true);
        }
        /// <summary>
        /// 发布消息异步方法，消息会被每个订阅者消费
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="exchange">路由器名</param>
        /// <param name="messageKey">消息关键字，若关键字有多个请用.分割</param>
        public void PublishToBuff<T>(T message, string exchange, string messageKey = "")
        {
            if (string.IsNullOrEmpty(exchange))
            {
                Log.LogError("PublishToBuff exchange is null", null, message);
                return;
            }
            BuffProducer buffProducer = GetBuffProducer(exchange, ProducerType_Publisher);
            buffProducer.PublishToBuff<T>(message, exchange, messageKey);
        }

        private bool SendInternal<T>(T message, string queue, SendOption option, bool sync)
        {
            QueueConfiguration queueConfig = null;
            if (BrokerConfig.QueueConfigs != null)
                queueConfig = BrokerConfig.QueueConfigs[queue];
            if (queueConfig == null)
            {
                Log.LogDebug(string.Format("queue config not found, {0}", queue), message);
            }
            IModel channel = Channel.GetChannel();
            if (channel == null)
                return false;
            try
            {
                Facility.DeclareQueue(queue, ref channel, queueConfig, true);
                byte[] body = SerializeMessage(message, queueConfig?.SerializerType);
                bool doConfirm = false;
                int confirmTimeout = 0;
                bool persistent = false;

                string replyTo = null;
                int priority = 0;
                string messageId = null;
                string correlationId = null;

                FindProducerOptions(queueConfig?.ProducerConfig, sync, out doConfirm, out confirmTimeout, out persistent);

                if (option != null)
                {
                    priority = option.Priority;
                    if (queueConfig != null)
                    {
                        if (priority > queueConfig.MaxPriority)
                            priority = queueConfig.MaxPriority;
                    }
                    messageId = option.MessageId;
                    correlationId = option.CorrelationId;
                    if (!string.IsNullOrEmpty(option.ReplyQueue))
                    {
                        replyTo = option.ReplyQueue;
                    }
                    else if (queueConfig?.ProducerConfig != null)
                    {
                        replyTo = queueConfig?.ProducerConfig.ReplyQueue;
                    }
                }

                bool success = DoSend(body, "", queue, channel, doConfirm, confirmTimeout, persistent, replyTo, priority, messageId, correlationId);
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

        private bool PublishInternal<T>(T message, string exchange, string messageKey, bool sync)
        {
            ExchangeConfiguration exchangeConfig = null;
            if (BrokerConfig.ExchangeConfigs != null)
            {
                exchangeConfig = BrokerConfig.ExchangeConfigs[exchange];
            }
            if (exchangeConfig == null)
            {
                Log.LogDebug(string.Format("exchange config not found, {0}", exchange), message);
            }
            IModel channel = Channel.GetChannel();
            if (channel == null)
                return false;
            try
            {
                Facility.DeclareExchange(exchange, channel, exchangeConfig);
                byte[] body = SerializeMessage(message, exchangeConfig?.SerializerType);
                bool doConfirm = false;
                int confirmTimeout = 0;
                bool persistent = false;
                FindProducerOptions(exchangeConfig?.ProducerConfig, sync, out doConfirm, out confirmTimeout, out persistent);
                bool success = DoSend(body, exchange, messageKey, channel, doConfirm, confirmTimeout, persistent);
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

        private void FindProducerOptions(ProducerConfiguration config, bool sync, out bool doConfirm, out int confirmTimeout, out bool persistent)
        {
            doConfirm = false;
            confirmTimeout = 0;
            persistent = false;
            if (config != null)
            {
                doConfirm = config.SendConfirm && sync;
                confirmTimeout = config.SendConfirmTimeout;
                persistent = config.MessagePersistent;
            }
        }

        private bool DoSend(byte[] message, string exchange, string routingKey, IModel channel, bool doConfirm = false, int confirmTimeout = 0, bool persistent = false, string replyTo = null, int priority = 0, string messageId = null, string correlationId = null)
        {
            IBasicProperties properties = null;
            if (!string.IsNullOrEmpty(replyTo) || persistent || priority > 0 || !string.IsNullOrEmpty(messageId) || !string.IsNullOrEmpty(correlationId))
            {
                properties = channel.CreateBasicProperties();
                properties.ReplyTo = replyTo ?? "";
                properties.Persistent = persistent;
                properties.Priority = (byte)priority;
                properties.MessageId = messageId ?? "";
                properties.CorrelationId = correlationId ?? "";
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

        private string GetProducerAction(int producerType)
        {
            switch (producerType)
            {
                case ProducerType_Sender:
                    return "send";
                case ProducerType_Publisher:
                    return "publish";
                default:
                    return null;
            }
        }

        BuffProducer GetBuffProducer(string name, int producerType)
        {
            string buffProducerName = GetProducerAction(producerType) + name;
            if (_buffProducerDict == null)
                _buffProducerDict = new Dictionary<string, BuffProducer>();
            if (!_buffProducerDict.ContainsKey(buffProducerName))
            {
                lock (_buffProducerDict)
                {
                    if (!_buffProducerDict.ContainsKey(buffProducerName))
                    {
                        int maxWorker = 1;
                        switch (producerType)
                        {
                            case ProducerType_Sender:
                                var queueConfig = BrokerConfig.QueueConfigs[name];
                                if (queueConfig != null && queueConfig.ProducerConfig != null)
                                {
                                    maxWorker = queueConfig.ProducerConfig.MaxWorker;
                                }
                                break;
                            case ProducerType_Publisher:
                                var exchangeConfig = BrokerConfig.ExchangeConfigs[name];
                                if (exchangeConfig != null && exchangeConfig.ProducerConfig != null)
                                {
                                    maxWorker = exchangeConfig.ProducerConfig.MaxWorker;
                                }
                                break;
                            default:
                                throw new NotImplementedException("producer type not supported");
                        }
                        BuffProducer buffProducer = new BuffProducer(this, maxWorker, producerType);
                        _buffProducerDict.Add(buffProducerName, buffProducer);
                    }
                }
            }
            return _buffProducerDict[buffProducerName];
        }

        class BuffProducer
        {
            private AutoResetEvent _resetEvent;
            private ConcurrentQueue<BuffMessage> _queue;
            private List<Thread> _workerList;
            private Producer _producer;
            public BuffProducer(Producer producer, int maxWorker, int producerType)
            {
                _producer = producer;
                _queue = new ConcurrentQueue<BuffMessage>();
                _resetEvent = new AutoResetEvent(false);

                if (maxWorker <= 0)
                    maxWorker = 1;
                _workerList = new List<Thread>(maxWorker);
                for (int i = 0; i < maxWorker; i++)
                {
                    Thread worker = null;
                    if (producerType == ProducerType_Sender)
                    {
                        worker = new Thread(DoSend);
                    }
                    else if (producerType == ProducerType_Publisher)
                    {
                        worker = new Thread(DoPublish);
                    }
                    worker.IsBackground = true;
                    worker.Start();
                    _workerList.Add(worker);
                }
            }

            internal void SendToBuff<T>(T message, string queue, SendOption option)
            {
                BuffMessage buffMessage = new BuffMessage();
                buffMessage.Message = message;
                buffMessage.Queue = queue;
                buffMessage.Option = option;

                _queue.Enqueue(buffMessage);
                _resetEvent.Set();
            }

            internal void PublishToBuff<T>(T message, string exchange, string messageKey)
            {
                BuffMessage buffMessage = new BuffMessage();
                buffMessage.Message = message;
                buffMessage.Exchange = exchange;
                buffMessage.MessageKey = messageKey;

                _queue.Enqueue(buffMessage);
                _resetEvent.Set();
            }

            private void DoSend()
            {
                ActionOnQueue((qm) => _producer.SendInternal<object>(qm.Message, qm.Queue, qm.Option, false));
            }

            private void DoPublish()
            {
                ActionOnQueue((qm) => _producer.PublishInternal<object>(qm.Message, qm.Exchange, qm.MessageKey, false));
            }

            private void ActionOnQueue(Func<BuffMessage, bool> action)
            {
                while (true)
                {
                    _resetEvent.Reset();
                    if (_queue.Count > 0)
                    {
                        BuffMessage qm = null;
                        while (_queue.TryDequeue(out qm))
                        {
                            if (!action(qm))
                            {
                                //todo send to failed queue
                            }
                            //SpinWait.SpinUntil(() => false, 1);
                        }
                    }
                    _resetEvent.WaitOne(10000);
                }
            }

            class BuffMessage
            {
                public object Message { get; set; }
                public string Queue { get; set; }

                public SendOption Option { get; set; }

                public string Exchange { get; set; }

                public string MessageKey { get; set; }
            }
        }
    }
}
