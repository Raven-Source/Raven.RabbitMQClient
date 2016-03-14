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
        internal BrokerConfiguration BrokerConfig { get; set; }

        internal FacilityManager Facility { get; set; }

        internal ILog Log { get; set; }

        internal ChannelManager Channel { get; set; }

        internal Consumer()
        {

        }

        public T ReceiveAndComplete<T>(string queue)
        {
            throw new NotImplementedException();
        }

        public List<T> ReceiveAndComplete<T>(string queue, int count)
        {
            if (count <= 0)
                return null;
            throw new NotImplementedException();
        }

        /// <summary>
        /// 注册消息队列事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        public bool OnReceive<T>(MessageReceived<T> callback, string queue)
        {
            return BindQueueEvent((ea, queueConfig, channel) =>
            {
                CommonHandler(callback, ea, queueConfig, channel);
            }, queue);
        }

        public bool OnReceive<TMessage, TReply>(MessageReceived<TMessage, TReply> callback, string queue, bool autoComplete)
        {
            return BindQueueEvent((ea, queueConfig, channel) =>
            {
                ReplyHandler<TMessage, TReply>(callback, ea, queueConfig, channel);
            }, queue);
        }
        /// <summary>
        /// 订阅消息队列
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="callback">消息回调</param>
        /// <param name="queue">队列名</param>
        /// <param name="autoComplete">接收消息自动完成</param>
        /// <param name="messageKey">消息关键字</param>
        /// <returns></returns>
        public bool Subscribe<T>(MessageReceived<T> callback, string queue)
        {
            return BindQueueEvent((ea, queueConfig, channel) =>
            {
                CommonHandler(callback, ea, queueConfig, channel);
            }, queue);
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

        private void CommonHandler<T>(MessageReceived<T> callback, BasicDeliverEventArgs ea, QueueConfiguration queueConfig, IModel channel)
        {
            var body = ea.Body;
            T message = DeserializeMessage<T>(body, queueConfig.SerializerType);
            if (!queueConfig.ConsumerConfig.ConsumeConfirm)
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            else if (callback(message, ea.RoutingKey, ea.BasicProperties?.MessageId, ea.BasicProperties?.CorrelationId))
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }

        private void ReplyHandler<TMessage, TReply>(MessageReceived<TMessage, TReply> callback, BasicDeliverEventArgs ea, QueueConfiguration queueConfig, IModel channel)
        {
            var body = ea.Body;
            TMessage message = DeserializeMessage<TMessage>(body, queueConfig?.SerializerType);
            
            if (!queueConfig.ConsumerConfig.ConsumeConfirm)
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            TReply reply = callback(message, ea.RoutingKey, ea.BasicProperties?.MessageId, ea.BasicProperties?.CorrelationId);
            if (queueConfig.ConsumerConfig.ConsumeConfirm)
            {
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            //todo send reply            
        }

        private bool BindQueueEvent(Action<BasicDeliverEventArgs, QueueConfiguration, IModel> handler, string queue)
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
                    Log.LogError(string.Format("BindQueueEvent queue config is empty, {0}", queue), null, null);
                    return false;
                }
                Facility.DeclareQueue(queue, channel, queueConfig);
                ushort workerCount = 10;
                if (queueConfig.ConsumerConfig.MaxWorker != null)
                    workerCount = queueConfig.ConsumerConfig.MaxWorker.Value;
                channel.BasicQos(prefetchSize: 0, prefetchCount: workerCount, global: false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    handler(ea, queueConfig, model as IModel);
                };
                channel.BasicConsume(queue: queue, noAck: !queueConfig.ConsumerConfig.ConsumeConfirm, consumer: consumer);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("BindQueueEvent failed, {0}", queue), ex, null);
                return false;
            }
        }
    }
}
