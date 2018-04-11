using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 消费者
    /// </summary>
    public class Consumer
    {
        readonly List<Tuple<string, Type, Type, object>> _receiveReplyEvents = new List<Tuple<string, Type, Type, object>>();
        readonly List<Tuple<string, Type, object>> _receiveEvents = new List<Tuple<string, Type, object>>();
        readonly List<Tuple<string, string, string, Type, object>> _subEvents = new List<Tuple<string, string, string, Type, object>>();
        internal event EventHandler ConsumerWorked;
        private readonly FacilityManager _facility;

        readonly ILog _log;

        private readonly ChannelManager _channel;
        private readonly ClientConfiguration _clientConfig;

        private readonly Producer _producer;

        internal Consumer(ChannelManager channel, ClientConfiguration config,ILog log, Producer producer,FacilityManager facility)
        {
            _facility = facility;
            _channel = channel;
            _log = log;
            _producer = producer;
            _clientConfig = config;
        }
        /// <summary>
        /// 查询队列中第一个消息，消息被查询到就会在队列中删除
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <returns>消息</returns>
        public T ReceiveAndComplete<T>(string queue)
        {
            if (string.IsNullOrEmpty(queue))
            {
                _log?.LogError("ReceiveAndComplete queue is null", null, null);
                return default(T);
            }
            T result = default(T);
            IModel channel = _channel.GetChannel();
            if (channel == null)
            {
                return default(T);
            }
            try
            {
                QueueConfiguration queueConfig = _clientConfig.QueueConfigs[queue];
                if (queueConfig != null)
                {
                    queue = queueConfig.Name;
                }
                BasicGetResult getResult = channel.BasicGet(queue, true);
                if (getResult != null)
                {
                    ConsumerWork();
                    result = DeserializeMessage<T>(getResult.Body, queueConfig?.SerializerType);
                }
                _channel.ReturnChannel(channel);
            }
            catch (Exception ex)
            {
                _log?.LogError($"ReceiveAndComplete failed, {queue}", ex, null);
                throw;
            }
            return result;
        }

        /// <summary>
        /// 查询队列中指定数量消息，消息被查询到就会在队列中删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<T> ReceiveAndComplete<T>(string queue, int count)
        {
            if (count <= 0)
            {
                _log?.LogError("ReceiveAndComplete count<=0", null, null);
                return null;
            }
            if (string.IsNullOrEmpty(queue))
            {
                _log?.LogError("ReceiveAndComplete queue is null", null, null);
                return null;
            }

            IModel channel = _channel.GetChannel();
            if (channel == null)
            {
                return null;
            }
            List<T> result = null;
            int i = 0;
            try
            {
                QueueConfiguration queueConfig = _clientConfig.QueueConfigs[queue];
                if (queueConfig != null)
                {
                    queue = queueConfig.Name;
                }
                for (; i < count; i++)
                {
                    BasicGetResult getResult = channel.BasicGet(queue, true);
                    if (getResult != null)
                    {
                        ConsumerWork();
                        T item = DeserializeMessage<T>(getResult.Body, queueConfig?.SerializerType);
                        if (result == null)
                            result = new List<T>(count);
                        result.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }
                _channel.ReturnChannel(channel);
            }
            catch (Exception ex)
            {
                _log?.LogError($"ReceiveAndComplete failed, queue: {queue}, expected: {count}, actual: {i}", ex, null);
                if (i == 0)
                    throw;
            }
            return result;
        }
        /// <summary>
        /// 订阅消息队列，队列收到消息后触发消息回调，消息回调构造回复消息，发送给回复队列
        /// 若回调方法未处理完成消息，消息将会被再次触发
        /// 回调方法在线程池中被调用，需要线程安全
        /// </summary>
        /// <typeparam name="TMessage">消息类型</typeparam>
        /// <typeparam name="TReply">回复消息类型</typeparam>
        /// <param name="queue">队列名</param>
        /// <param name="callback">消息回调</param>
        /// <returns>订阅成功</returns>
        public bool OnReceive<TMessage, TReply>(string queue, MessageReceived<TMessage, TReply> callback)
        {
            bool success = BindQueueEvent(queue, null, null, (ea, queueConfig, channel) =>
             {
                 ReplyHandler(callback, ea, queueConfig, channel);
             });
            if (success)
            {
                _receiveReplyEvents.Add(new Tuple<string, Type, Type, object>(queue, typeof(TMessage), typeof(TReply), callback));
            }
            return success;
        }
        /// <summary>
        /// 订阅消息队列，队列收到消息后触发消息回调
        /// 若回调方法未处理完成消息，消息将会被再次触发
        /// 回调方法在线程池中被调用，需要线程安全
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="callback">消息回调</param>
        /// <param name="queue">队列名</param>
        /// <returns>订阅成功</returns>
        public bool OnReceive<T>(string queue, MessageReceived<T> callback)
        {
            bool success = BindQueueEvent(queue, null, null, (ea, queueConfig, channel) =>
             {
                 CommonHandler(callback, ea, queueConfig, channel);
             });
            if (success)
            {
                _receiveEvents.Add(new Tuple<string, Type, object>(queue, typeof(T), callback));
            }
            return success;
        }
        /// <summary>
        /// 订阅消息队列，队列收到消息后触发消息回调
        /// 若回调方法未处理完成消息，消息将会被再次触发
        /// 回调方法在线程池中被调用，需要线程安全
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="exchange">路由器名</param>
        /// <param name="queue">队列名</param>
        /// <param name="messageKeyPattern">消息关键字模式</param>
        /// <param name="callback">消息回调</param>
        /// <returns>订阅成功</returns>
        public bool Subscribe<T>(string exchange, string queue, string messageKeyPattern, MessageReceived<T> callback)
        {
            bool success = BindQueueEvent(queue, exchange, messageKeyPattern, (ea, queueConfig, channel) =>
              {
                  CommonHandler(callback, ea, queueConfig, channel);
              });
            if (success)
            {
                _subEvents.Add(new Tuple<string, string, string, Type, object>(exchange, queue, messageKeyPattern, typeof(T), callback));
            }
            return success;
        }

        internal void Recover(Consumer consumer)
        {
            Type consumerType = typeof(Consumer);
            foreach (var i in _receiveReplyEvents)
            {
                MethodInfo method = consumerType.GetMethods().First((f) => f.Name == nameof(OnReceive) && f.GetParameters()[1].ParameterType.GenericTypeArguments.Length == 2);
                method = method.MakeGenericMethod(i.Item2, i.Item3);
                method.Invoke(consumer, new [] { i.Item1, i.Item4 });
            }
            foreach (var i in _receiveEvents)
            {
                var method = consumerType.GetMethods().First((f) => f.Name == nameof(OnReceive) && f.GetParameters()[1].ParameterType.GenericTypeArguments.Length == 1);
                method = method.MakeGenericMethod(i.Item2);
                method.Invoke(consumer, new [] { i.Item1, i.Item3 });
            }
            foreach (var i in _subEvents)
            {
                var method = consumerType.GetMethod(nameof(Subscribe));
                if (method != null)
                {
                    method = method.MakeGenericMethod(i.Item4);
                    method.Invoke(consumer, new [] {i.Item1, i.Item2, i.Item3, i.Item5});
                }
            }
        }

        private bool BindQueueEvent(string queue, string exchange, string messageKeyPattern, Action<BasicDeliverEventArgs, QueueConfiguration, IModel> handler)
        {
            if (string.IsNullOrEmpty(queue))
            {
                _log?.LogError("BindQueueEvent queue is null", null, null);
                return false;
            }
            IModel channel = _channel.GetChannel();
            if (channel == null)
            {
                _log?.LogError($"BindQueueEvent channel is empty, {queue}", null, null);
                return false;
            }
            try
            {
               _clientConfig.QueueConfigs.TryGetValue(queue,out var queueConfig);
                if (queueConfig == null)
                {
                    _log?.LogDebug($"BindQueueEvent queue config is empty, {queue}", null);
                    queueConfig=new QueueConfiguration(queue,bindToExchange:exchange,producerConfig:new ProducerConfiguration());
                }
                if (!string.IsNullOrEmpty(exchange))
                {
                    _facility.DeclareQueueAndBindExchange(ref queue, ref channel, queueConfig, ref exchange, messageKeyPattern);
                }
                else
                {
                    _facility.DeclareQueue(ref queue, ref channel, queueConfig, false);
                }

                var workerCount = queueConfig.MaxWorker;
                channel.BasicQos(prefetchSize: 0, prefetchCount:workerCount, global: false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    ConsumerWork();
                    if (model is EventingBasicConsumer c)
                    {
                        var ch = c.Model;
                        handler(ea, queueConfig, ch);
                    }
                };

                channel.BasicConsume(queue: queue, autoAck: !NeedAck(queueConfig), consumer: consumer);
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"BindQueueEvent failed, {queue}", ex, null);
                return false;
            }
        }

        private void CommonHandler<T>(MessageReceived<T> callback, BasicDeliverEventArgs ea, QueueConfiguration queueConfig, IModel channel)
        {
            var message = default(T);
            var success = false;
            try
            {
                var body = ea.Body;
                message = DeserializeMessage<T>(body, queueConfig?.SerializerType);
                _log?.LogDebug("message received", message);
                success = callback(message, ea.RoutingKey, ea.BasicProperties?.MessageId, ea.BasicProperties?.CorrelationId, ea);
            }
            catch (Exception ex)
            {
                _log?.LogError("CommonHandler callback exception", ex, message);
            }
            if (NeedAck(queueConfig))
            {
                if (success)
                {
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                else
                {
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        }

        private void ReplyHandler<TMessage, TReply>(MessageReceived<TMessage, TReply> callback, BasicDeliverEventArgs ea, QueueConfiguration queueConfig, IModel channel)
        {
            var body = ea.Body;
            var message = DeserializeMessage<TMessage>(body, queueConfig?.SerializerType);
            _log?.LogDebug("message received", message);
            var needAck = NeedAck(queueConfig);
            TReply reply;
            try
            {
                reply = callback(message, ea.RoutingKey, ea.BasicProperties?.MessageId, ea.BasicProperties?.CorrelationId, ea);
            }
            catch (Exception ex)
            {
                _log?.LogError("ReplyHandler callback exception", ex, message);
                return;
            }
            if (needAck)
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            if (!string.IsNullOrEmpty(ea.BasicProperties?.ReplyTo))
            {
                SendOption option = null;
                if (!string.IsNullOrEmpty(ea.BasicProperties.MessageId))
                {
                    option = new SendOption {CorrelationId = ea.BasicProperties.MessageId};
                }
                _producer.SendToBuff(reply, ea.BasicProperties.ReplyTo, option);
            }
        }

        private T DeserializeMessage<T>(byte[] message, SerializerType? serializerType)
        {
            var sType = serializerType ?? _clientConfig.SerializerType;
            return SerializerService.Deserialize<T>(message, sType);
        }

        private bool NeedAck(QueueConfiguration queueConfig)
        {
            return queueConfig?.NeedAck == true;
        }

        private void ConsumerWork()
        {
            ConsumerWorked?.Invoke(this, null);
        }
    }
}
