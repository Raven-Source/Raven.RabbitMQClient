using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    public class ExchangeConfiguration : ConfigurationElement
    {
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
        /// 消息匹配类型，默认为topic
        /// fanout，所有消息会被绑定队列消费
        /// headers，多条件匹配规则，客户端方法暂不支持
        /// direct，消息关键字完全匹配
        /// topic，消息关键字模式匹配
        /// </summary>
        [ConfigurationProperty("serializerType")]
        public string ExchangeType
        {
            get
            {
                return (string)this["exchangeType"];
            }
            set
            {
                this["exchangeType"] = value;
            }
        }

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
        public ProducerConfiguration ProducerConfig
        {
            get
            {
                return (ProducerConfiguration)this["producer"];
            }
            set
            {
                this["producer"] = value;
            }
        }
    }
    public class ExchangeConfigurationCollection : ConfigurationElementCollection
    {
        public new ExchangeConfiguration this[string exchangeName]
        {
            get
            {
                return this.BaseGet(exchangeName) as ExchangeConfiguration;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ExchangeConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExchangeConfiguration)element).Name;
        }
    }
}
