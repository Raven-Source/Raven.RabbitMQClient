using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using Raven.Serializer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 生产者
    /// </summary>
    public class Producer
    {
        const int ProducerTypeSender = 0;
        const int ProducerTypePublisher = 1;
        private static readonly SendOption DelaySendOption = new SendOption { Delay = true };

        internal event EventHandler ProducerWorked;
        

        private readonly ILog _log;
        private readonly ChannelManager _channel;
        private readonly ClientConfiguration _clientConfiguration;
        private readonly FacilityManager _facility;

        internal Producer(ChannelManager channel,ClientConfiguration config,ILog log, FacilityManager facility)
        {
            _channel = channel;
            _clientConfiguration = config;
            _log = log;
            _facility = facility;
        }

        Dictionary<string, BuffProducer> _buffProducerDict = null;
        /// <summary>
        /// 发送延迟消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        public bool SendDelay<T>(T message, string queue)
        {
            return Send(message, queue, DelaySendOption);
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
            if (string.IsNullOrEmpty(queue))
            {
                _log?.LogError("Send queue is null", null, message);
                return false;
            }
            return SendInternal(message, queue, option, true);
        }
        /// <summary>
        /// 发送延迟消息非阻塞方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="queue"></param>
        public void SendDelayToBuff<T>(T message, string queue)
        {
            SendToBuff(message, queue, DelaySendOption);
        }
        /// <summary>
        /// 发送非阻塞方法，往指定队列发送消息，消息只会被消费一次
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="queue">队列名</param>
        /// <param name="option">附加参数 <see cref="Raven.Message.RabbitMQ.SendOption"/></param>
        public void SendToBuff<T>(T message, string queue, SendOption option = null)
        {
            if (string.IsNullOrEmpty(queue))
            {
                _log?.LogError("SendToBuff queue is null", null, message);
                return;
            }
            BuffProducer producer = GetBuffProducer(queue, ProducerTypeSender);
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
                _log?.LogError("Publish exchange is null", null, message);
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
                _log?.LogError("PublishToBuff exchange is null", null, message);
                return;
            }
            BuffProducer buffProducer = GetBuffProducer(exchange, ProducerTypePublisher);
            buffProducer.PublishToBuff<T>(message, exchange, messageKey);
        }

        private bool SendInternal<T>(T message, string queue, SendOption option, bool sync)
        {
            QueueConfiguration queueConfig = null;
            if (_clientConfiguration.QueueConfigs != null)
                queueConfig = _clientConfiguration.QueueConfigs[queue];
            if (queueConfig == null)
            {
                _log?.LogDebug($"queue config not found, {queue}", message);
            }
            IModel channel = _channel.GetChannel();
            if (channel == null)
                return false;
            try
            {
                _facility.DeclareQueue(ref queue, ref channel, queueConfig, true);
                byte[] body = SerializeMessage(message, queueConfig?.SerializerType);
                bool doConfirm = false;
                int confirmTimeout = 0;
                bool persistent = false;
                string sendQueue = queue;
                int messageDelay = 0;

                string replyTo = null;
                int priority = 0;
                string messageId = null;
                string correlationId = null;

                FindProducerOptions(queueConfig?.ProducerConfig, sync, out doConfirm, out confirmTimeout, out persistent, out messageDelay);

                if (option != null)
                {
                    priority = option.Priority;
                    if (priority > queueConfig?.MaxPriority)
                        priority = queueConfig.MaxPriority;
                    messageId = option.MessageId;
                    correlationId = option.CorrelationId;
                    if (!string.IsNullOrEmpty(option.ReplyQueue))
                    {
                        replyTo = option.ReplyQueue;
                    }
                    else if (queueConfig != null)
                    {
                        replyTo = queueConfig.ReplyQueue;
                    }
                    if (messageDelay > 0 && option.Delay)
                    {
                        sendQueue = CreateDelayProxyQueue(ref channel, "", queue, messageDelay);
                    }
                }

                bool success = DoSend(body, "", sendQueue, channel, doConfirm, confirmTimeout, persistent, replyTo, priority, messageId, correlationId);
                if (success)
                {
                    _log?.LogDebug($"send success, {sendQueue}", message);
                    _channel.ReturnChannel(channel);
                }
                else
                {
                    _log?.LogError($"send failed, {sendQueue}", null, message);
                }
                return success;
            }
            catch (Exception ex)
            {
                _log?.LogError($"send exception, {queue}", ex, message);
                return false;
            }
        }

        private string CreateDelayProxyQueue(ref IModel channel, string exchange, string routeKey, int delay)
        {
            string delayQueue = $"delay_{exchange}_{routeKey}_{delay}";
            QueueConfiguration delayQueueConfig = new QueueConfiguration(delayQueue);
            delayQueueConfig.Name = delayQueue;
            delayQueueConfig.Expiration = (uint)delay;
            delayQueueConfig.Durable = true;
            delayQueueConfig.RedeclareWhenFailed = true;
            delayQueueConfig.DeadExchange = string.IsNullOrEmpty(exchange) ? "amq.direct" : exchange;
            delayQueueConfig.DeadMessageKeyPattern = routeKey;
            _facility.DeclareQueue(ref delayQueue, ref channel, delayQueueConfig, true);
            _facility.DeclareBind(channel, routeKey, exchange, routeKey);
            return delayQueue;
        }

        private bool PublishInternal<T>(T message, string exchange, string messageKey, bool sync)
        {
            ExchangeConfiguration exchangeConfig = null;
            if (_clientConfiguration.ExchangeConfigurations != null)
            {
                exchangeConfig = _clientConfiguration.ExchangeConfigurations[exchange];
            }
            if (exchangeConfig == null)
            {
                _log?.LogDebug($"exchange config not found, {exchange}", message);
            }
            IModel channel = _channel.GetChannel();
            if (channel == null)
                return false;
            try
            {
                _facility.DeclareExchange(ref exchange, channel, exchangeConfig);
                byte[] body = SerializeMessage(message, exchangeConfig?.SerializerType);
                bool doConfirm = false;
                int confirmTimeout = 0;
                bool persistent = false;
                int messageDelay = 0;
                FindProducerOptions(exchangeConfig?.ProducerConfig, sync, out doConfirm, out confirmTimeout, out persistent, out messageDelay);
                bool success = DoSend(body, exchange, messageKey, channel, doConfirm, confirmTimeout, persistent);
                if (success)
                {
                    _log?.LogDebug($"publish success, {exchange} {messageKey}", message);
                    _channel.ReturnChannel(channel);
                }
                else
                {
                    _log?.LogError($"publish failed, {exchange} {messageKey}", null, message);
                }
                return success;
            }
            catch (Exception ex)
            {
                _log?.LogError($"publish failed, {exchange} {messageKey}", ex, message);
                return false;
            }
        }

        private void FindProducerOptions(ProducerConfiguration config, bool sync, out bool doConfirm, out int confirmTimeout, out bool persistent, out int messageDelay)
        {
            messageDelay = 0;
            doConfirm = false;
            confirmTimeout = 0;
            persistent = false;
            if (config != null)
            {
                doConfirm = config.SendConfirm && sync;
                confirmTimeout = config.SendConfirmTimeout;
                persistent = config.MessagePersistent;
                messageDelay = config.MessageDelay;
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
            ProducerWork();
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
            var sType = serializerType ?? _clientConfiguration.SerializerType;
            return SerializerService.Serialize(message, sType);
        }

        private string GetProducerAction(int producerType)
        {
            switch (producerType)
            {
                case ProducerTypeSender:
                    return "send";
                case ProducerTypePublisher:
                    return "publish";
                default:
                    return null;
            }
        }

        private BuffProducer GetBuffProducer(string name, int producerType)
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
                            case ProducerTypeSender:
                                var queueConfig = _clientConfiguration.QueueConfigs[name];
                                if (queueConfig != null )
                                {
                                    maxWorker = queueConfig.MaxWorker;
                                }
                                break;
                            case ProducerTypePublisher:
                                var exchangeConfig = _clientConfiguration.ExchangeConfigurations[name];
                                if (exchangeConfig?.ProducerConfig != null)
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

        private void ProducerWork()
        {
            ProducerWorked?.Invoke(this, null);
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
                    if (producerType == ProducerTypeSender)
                    {
                        worker = new Thread(DoSend);
                    }
                    else if (producerType == ProducerTypePublisher)
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
