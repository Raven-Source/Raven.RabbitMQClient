using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    /// <summary>
    /// 队列配置
    /// </summary>
    public class QueueConfiguration : ConfigurationElement
    {
        #region 队列定义
        /// <summary>
        /// 名字
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }
        /// <summary>
        /// 当没有消费者时，队列是否自动删除
        /// </summary>
        [ConfigurationProperty("autoDelete")]
        public bool AutoDelete
        {
            get
            {
                return (bool)this["autoDelete"];
            }
            set
            {
                this["autoDelete"] = value;
            }
        }
        /// <summary>
        /// 队列可重用，在消息中间件重启后队列是否还能继续使用
        /// </summary>
        [ConfigurationProperty("durable")]
        public bool Durable
        {
            get
            {
                return (bool)this["durable"];
            }
            set
            {
                this["durable"] = value;
            }
        }
        /// <summary>
        /// 支持最大优先级，最小0，最大10
        /// </summary>
        [ConfigurationProperty("maxPriority")]
        public byte MaxPriority
        {
            get
            {
                return (byte)this["maxPriority"];
            }
            set
            {
                this["maxPriority"] = value;
            }
        }
        /// <summary>
        /// 过期时间，毫秒为单位，在定义队列时加入x-message-ttl参数
        /// </summary>
        [ConfigurationProperty("expiration")]
        public int Expiration
        {
            get
            {
                return (int)this["expiration"];
            }
            set
            {
                this["expiration"] = value;
            }
        }
        /// <summary>
        /// 最大长度，当达到最大长度后开始从头部删除消息
        /// </summary>
        [ConfigurationProperty("maxLength")]
        public int MaxLength
        {
            get
            {
                return (int)this["maxLength"];
            }
            set
            {
                this["maxLength"] = value;
            }
        }
        /// <summary>
        /// 当定义队列失败时，对队列重新定义，注意重新定义会删除队列
        /// </summary>
        [ConfigurationProperty("redeclareWhenFailed")]
        public bool RedeclareWhenFailed
        {
            get
            {
                return (bool)this["redeclareWhenFailed"];
            }
            set
            {
                this["redeclareWhenFailed"] = value;
            }
        }
        /// <summary>
        /// 队列需要绑定到交换器
        /// </summary>
        [ConfigurationProperty("bindToExchange")]
        public string BindToExchange { get; set; }
        /// <summary>
        /// 绑定消息关键字
        /// </summary>
        [ConfigurationProperty("bindMessageKey")]
        public string BindMessageKey { get; set; }
        #endregion

        /// <summary>
        /// 序列化类型，如果配置此项将覆盖客户端配置中的序列化类型
        /// </summary>
        [ConfigurationProperty("serializerType")]
        public SerializerType? SerializerType
        {
            get
            {
                return (SerializerType?)this["serializerType"];
            }
            set
            {
                this["serializerType"] = value;
            }
        }

        [ConfigurationProperty("producer")]
        public QueueProducerConfiguration ProducerConfig
        {
            get
            {
                return (QueueProducerConfiguration)this["producer"];
            }
            set
            {
                this["producer"] = value;
            }
        }

        [ConfigurationProperty("consumer")]
        public QueueConsumerConfiguration ConsumerConfig
        {
            get
            {
                return (QueueConsumerConfiguration)this["consumer"];
            }
            set
            {
                this["consumer"] = value;
            }
        }
    }

    /// <summary>
    /// 队列发送行为
    /// </summary>
    public class QueueProducerConfiguration : ConfigurationElement
    {
        /// <summary>
        /// 只有当消息中间件确认已送达才算发送成功，确保消息不会因为网络或服务异常等原因造成少发的情况
        /// 在等待消息确认时有一定延迟，若超过超时时间则认为发送失败
        /// </summary>
        [ConfigurationProperty("sendConfirm")]
        public bool SendConfirm
        {
            get
            {
                return (bool)this["sendConfirm"];
            }
            set
            {
                this["sendConfirm"] = value;
            }
        }
        /// <summary>
        /// 确认超时时间，单位毫秒
        /// </summary>
        [ConfigurationProperty("sendConfirmTimeout")]
        public int SendConfirmTimeout
        {
            get
            {
                return (int)this["sendConfirmTimeout"];
            }
            set
            {
                this["sendConfirmTimeout"] = value;
            }
        }
        /// <summary>
        /// 回复队列名
        /// </summary>
        [ConfigurationProperty("replyQueue")]
        public string ReplyQueue
        {
            get
            {
                return (string)this["replyQueue"];
            }
            set
            {
                this["replyQueue"] = value;
            }
        }
        /// <summary>
        /// 消息持久化
        /// </summary>
        [ConfigurationProperty("messagePersistent")]
        public bool MessagePersistent
        {
            get
            {
                return (bool)this["messagePersistent"];
            }
            set
            {
                this["messagePersistent"] = value;
            }
        }
    }

    /// <summary>
    /// 队列消费行为
    /// </summary>
    public class QueueConsumerConfiguration : ConfigurationElement
    {
        /// <summary>
        /// 发送消费确认，如果确认消息发送失败，消息会被重复消费
        /// </summary>
        [ConfigurationProperty("consumeConfirm")]
        public bool ConsumeConfirm
        {
            get
            {
                return (bool)this["consumeConfirm"];
            }
            set
            {
                this["consumeConfirm"] = value;
            }
        }
        /// <summary>
        /// 最多同时处理消息数量
        /// </summary>
        [ConfigurationProperty("maxWorker")]
        public ushort? MaxWorker
        {
            get
            {
                return (ushort?)this["maxWorker"];
            }
            set
            {
                this["maxWorker"] = value;
            }
        }
        /// <summary>
        /// 消息监听器类型，接口<see cref="Raven.Message.RabbitMQ.Abstract.IMessageListener{T}"/>的实现类，若配置此项，初始化后自动监听队列
        /// </summary>
        [ConfigurationProperty("messageListenerType")]
        public string MessageListenerType
        {
            get
            {
                return (string)this["messageListenerType"];
            }
            set
            {
                this["messageListenerType"] = value;
            }
        }
    }

    public class QueueConfigurationCollection : ConfigurationElementCollection
    {
        public new QueueConfiguration this[string queueName]
        {
            get
            {
                return this.BaseGet(queueName) as QueueConfiguration;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new QueueConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((QueueConfiguration)element).Name;
        }
    }
}
