using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 消费者
    /// </summary>
    public class Consumer
    {
        internal const int DefaultMaxWorker = 10;
        internal BrokerConfiguration BrokerConfig { get; set; }

        internal FacilityManager Facility { get; set; }

        internal ILog Log { get; set; }

        internal ChannelManager Channel { get; set; }

        internal Client Client { get; set; }

        internal Consumer()
        {

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
                Log.LogError("ReceiveAndComplete queue is null", null, null);
                return default(T);
            }
            T result = default(T);
            IModel channel = Channel.GetChannel();
            if (channel == null)
            {
                return default(T);
            }
            try
            {
                QueueConfiguration queueConfig = BrokerConfig.QueueConfigs[queue];
                BasicGetResult getResult = channel.BasicGet(queue, true);
                if (getResult != null)
                {
                    result = DeserializeMessage<T>(getResult.Body, queueConfig?.SerializerType);
                }
                Channel.ReturnChannel(channel);
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("ReceiveAndComplete failed, {0}", queue), ex, null);
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
                Log.LogError("ReceiveAndComplete count<=0", null, null);
                return null;
            }
            if (string.IsNullOrEmpty(queue))
            {
                Log.LogError("ReceiveAndComplete queue is null", null, null);
                return null;
            }

            IModel channel = Channel.GetChannel();
            if (channel == null)
            {
                return null;
            }
            List<T> result = null;
            int i = 0;
            try
            {
                QueueConfiguration queueConfig = BrokerConfig.QueueConfigs[queue];
                for (; i < count; i++)
                {
                    BasicGetResult getResult = channel.BasicGet(queue, true);
                    if (getResult != null)
                    {
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
                Channel.ReturnChannel(channel);
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("ReceiveAndComplete failed, queue: {0}, expected: {1}, actual: {2}", queue, count, i), ex, null);
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
            return BindQueueEvent(queue, null, null, (ea, queueConfig, channel) =>
             {
                 ReplyHandler<TMessage, TReply>(callback, ea, queueConfig, channel);
             });
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
            return BindQueueEvent(queue, null, null, (ea, queueConfig, channel) =>
             {
                 CommonHandler(callback, ea, queueConfig, channel);
             });
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
            return BindQueueEvent(queue, exchange, messageKeyPattern, (ea, queueConfig, channel) =>
              {
                  CommonHandler(callback, ea, queueConfig, channel);
              });
        }

        private bool BindQueueEvent(string queue, string exchange, string messageKeyPattern, Action<BasicDeliverEventArgs, QueueConfiguration, IModel> handler)
        {
            IModel channel = Channel.GetChannel();
            if (channel == null)
            {
                Log.LogError(string.Format("BindQueueEvent channel is empty, {0}", queue), null, null);
                return false;
            }
            try
            {
                QueueConfiguration queueConfig = BrokerConfig.QueueConfigs[queue];
                if (queueConfig == null)
                {
                    Log.LogDebug(string.Format("BindQueueEvent queue config is empty, {0}", queue), null);
                }
                if (!string.IsNullOrEmpty(exchange))
                {
                    Facility.DeclareQueueAndBindExchange(queue, ref channel, queueConfig, exchange, messageKeyPattern);
                }
                int workerCount = DefaultMaxWorker;
                if (queueConfig != null && queueConfig.ConsumerConfig != null)
                {
                    workerCount = queueConfig.ConsumerConfig.MaxWorker;
                }
                channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)workerCount, global: false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    EventingBasicConsumer c = model as EventingBasicConsumer;
                    IModel ch = c.Model;
                    handler(ea, queueConfig, ch);
                };

                channel.BasicConsume(queue: queue, noAck: NoAck(queueConfig), consumer: consumer);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("BindQueueEvent failed, {0}", queue), ex, null);
                return false;
            }
        }

        private void CommonHandler<T>(MessageReceived<T> callback, BasicDeliverEventArgs ea, QueueConfiguration queueConfig, IModel channel)
        {
            var body = ea.Body;
            T message = DeserializeMessage<T>(body, queueConfig?.SerializerType);
            Log.LogDebug("message received", message);
            if (NoAck(queueConfig))
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            else if (callback(message, ea.RoutingKey, ea.BasicProperties?.MessageId, ea.BasicProperties?.CorrelationId, ea))
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }

        private void ReplyHandler<TMessage, TReply>(MessageReceived<TMessage, TReply> callback, BasicDeliverEventArgs ea, QueueConfiguration queueConfig, IModel channel)
        {
            var body = ea.Body;
            TMessage message = DeserializeMessage<TMessage>(body, queueConfig?.SerializerType);
            Log.LogDebug("message received", message);
            bool noAck = NoAck(queueConfig);
            if (noAck)
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            TReply reply = callback(message, ea.RoutingKey, ea.BasicProperties?.MessageId, ea.BasicProperties?.CorrelationId, ea);
            if (!noAck)
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            if (ea.BasicProperties != null && !string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
            {
                SendOption option = null;
                if (!string.IsNullOrEmpty(ea.BasicProperties.MessageId))
                {
                    option = new SendOption();
                    option.CorrelationId = ea.BasicProperties.MessageId;
                }
                Client.Producer.SendToBuff<TReply>(reply, ea.BasicProperties.ReplyTo, option);
            }
        }

        private T DeserializeMessage<T>(byte[] message, SerializerType? serializerType)
        {
            SerializerType sType;
            if (serializerType != null)
                sType = serializerType.Value;
            else
                sType = ClientConfiguration.Instance.SerializerType;
            return SerializerService.Deserialize<T>(message, sType);
        }

        private bool NoAck(QueueConfiguration queueConfig)
        {
            bool noAck = false;
            if (queueConfig != null && queueConfig.ConsumerConfig != null)
            {
                noAck = !queueConfig.ConsumerConfig.ConsumeConfirm;
            }
            return noAck;
        }
    }
}
