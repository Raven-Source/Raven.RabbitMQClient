using RabbitMQ.Client;
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

        public void SendToBuff<T>(T message, string queue, SendOption option = null)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 发布消息，消息会被每个订阅者消费
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息</param>
        /// <param name="exchange">路由器名</param>
        /// <param name="messageKey">消息关键字，若关键字有多个请用.分割</param>
        public bool Publish<T>(T message, string exchange, string messageKey = null, SendOption option = null)
        {
            throw new NotImplementedException();
        }

        private bool SendInternal<T>(T message, string queue, SendOption option, bool sync)
        {
            QueueConfiguration queueConfig = BrokerConfig.QueueConfigs[queue];
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
                bool success = DoSend(message, null, queue, channel, queueConfig, option, sync);
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

        private bool DoSend<T>(T message, string exchange, string routingKey, IModel channel, QueueConfiguration queueConfig, SendOption option, bool doConfirm)
        {

            bool needConfirm = false;
            int confirmTimeout = 0;
            bool confirmed = false;
            IBasicProperties properties = null;
            SerializerType serializerType = ClientConfiguration.Instance.SerializerType;

            if (queueConfig != null)
            {
                if (queueConfig.SerializerType != null)
                    serializerType = queueConfig.SerializerType.Value;
                if (queueConfig.ProducerConfig != null)
                {
                    needConfirm = queueConfig.ProducerConfig.SendConfirm && doConfirm;
                    confirmTimeout = queueConfig.ProducerConfig.SendConfirmTimeout;
                    if (queueConfig.ProducerConfig.MessagePersistent || !string.IsNullOrEmpty(queueConfig.ProducerConfig.ReplyQueue))
                    {
                        properties = channel.CreateBasicProperties();
                        properties.ReplyTo = queueConfig.ProducerConfig.ReplyQueue;
                        properties.Persistent = queueConfig.ProducerConfig.MessagePersistent;
                    }
                }
            }
            if (needConfirm)
            {
                channel.ConfirmSelect();
            }

            if (option != null)
            {
                if (properties == null)
                    properties = channel.CreateBasicProperties();
                properties.Priority = option.Priority;
                properties.MessageId = option.MessageId;
                properties.CorrelationId = option.CorrelationId;
            }
            var body = SerializerService.Serialize(message, serializerType);
            channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: properties, body: body);
            if (needConfirm)
            {
                confirmed = channel.WaitForConfirms(TimeSpan.FromMilliseconds(confirmTimeout));
                return confirmed;
            }
            return true;
        }
    }
}
