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
    public class QueueConfiguration : DeclareConfiguration
    {
        #region 队列定义

        /// <summary>
        /// 支持最大优先级，最小0，最大10，默认0
        /// </summary>
        [ConfigurationProperty("maxPriority", DefaultValue = 0)]
        [IntegerValidator(ExcludeRange = false, MaxValue = 10, MinValue = 0)]
        public int MaxPriority
        {
            get
            {
                return (int)this["maxPriority"];
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
        //[LongValidator(ExcludeRange = false, MaxValue = long.MaxValue, MinValue = 1)]
        public uint? Expiration
        {
            get
            {
                return (uint?)this["expiration"];
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
        //[LongValidator(ExcludeRange = false, MaxValue = long.MaxValue, MinValue = 1)]
        public uint? MaxLength
        {
            get
            {
                return (uint?)this["maxLength"];
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
        public string BindToExchange
        {
            get
            {
                return (string)this["bindToExchange"];
            }
            set
            {
                this["bindToExchange"] = value;
            }
        }
        /// <summary>
        /// 绑定消息关键字模式
        /// </summary>
        [ConfigurationProperty("bindMessageKeyPattern")]
        public string BindMessageKeyPattern
        {
            get
            {
                return (string)this["bindMessageKeyPattern"];
            }
            set
            {
                this["bindMessageKeyPattern"] = value;
            }
        }
        /// <summary>
        /// 消息拒绝或过期后推送到指定路由
        /// </summary>
        internal string DeadExchangeInternal { get; set; }
        /// <summary>
        /// 消息拒绝或过期后路由关键字
        /// </summary>
        internal string DeadMessageKeyPatternInternal { get; set; }
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
    public class QueueProducerConfiguration : ProducerConfiguration
    {
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
    }

    /// <summary>
    /// 队列消费行为
    /// </summary>
    public class QueueConsumerConfiguration : ConfigurationElement
    {
        public QueueConsumerConfiguration()
        {
            MaxWorker = Consumer.DefaultMaxWorker;
        }
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
        /// 最多同时处理消息数量，默认值10
        /// </summary>
        [ConfigurationProperty("maxWorker", DefaultValue = Consumer.DefaultMaxWorker)]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
        public int MaxWorker
        {
            get
            {
                return (int)this["maxWorker"];
            }
            set
            {
                this["maxWorker"] = value;
            }
        }
        /// <summary>
        /// 消息处理失败后重试的间隔，单位毫秒
        /// </summary>
        [ConfigurationProperty("retryInterval")]
        public int? RetryInterval
        {
            get
            {
                return (int?)this["retryInterval"];
            }
            set
            {
                this["retryInterval"] = value;
            }
        }
        [ConfigurationProperty("skipRetry", DefaultValue = false)]
        public bool SkipRetry
        {
            get
            {
                return (bool)this["skipRetry"];
            }
            set
            {
                this["skipRetry"] = value;
            }
        }

        ///// <summary>
        ///// 消息监听器类型，接口<see cref="Raven.Message.RabbitMQ.Abstract.IMessageListener{T}"/>的实现类，若配置此项，初始化后自动监听队列
        ///// </summary>
        //[ConfigurationProperty("messageListenerType")]
        //public string MessageListenerType
        //{
        //    get
        //    {
        //        return (string)this["messageListenerType"];
        //    }
        //    set
        //    {
        //        this["messageListenerType"] = value;
        //    }
        //}
    }

    public class QueueConfigurationCollection : EditableConfigurationElementCollection
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
