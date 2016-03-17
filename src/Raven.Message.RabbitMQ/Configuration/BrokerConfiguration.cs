using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    /// <summary>
    /// 服务器配置
    /// </summary>
    public class BrokerConfiguration : ConfigurationElement
    {
        /// <summary>
        /// 别名
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
        /// 域名
        /// </summary>
        [ConfigurationProperty("host", IsRequired = true)]
        public string Host
        {
            get
            {
                return (string)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }
        /// <summary>
        /// 用户名
        /// </summary>
        [ConfigurationProperty("userName")]
        public string UserName
        {
            get
            {
                return (string)this["userName"];
            }
            set
            {
                this["userName"] = value;
            }
        }
        /// <summary>
        /// 密码
        /// </summary>
        [ConfigurationProperty("password")]
        public string Password
        {
            get
            {
                return (string)this["password"];
            }
            set
            {
                this["password"] = value;
            }
        }
        /// <summary>
        /// 端口
        /// </summary>
        [ConfigurationProperty("port")]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
        public int? Port
        {
            get
            {
                return (int?)this["port"];
            }
            set
            {
                this["port"] = value;
            }
        }
        /// <summary>
        /// 队列配置
        /// </summary>
        [ConfigurationProperty("queues")]
        [ConfigurationCollection(typeof(QueueConfiguration), AddItemName = "queue")]
        public QueueConfigurationCollection QueueConfigs
        {
            get
            {
                return (QueueConfigurationCollection)this["queues"];
            }
            set
            {
                this["queues"] = value;
            }
        }
        /// <summary>
        /// 路由器配置
        /// </summary>
        [ConfigurationProperty("exchanges")]
        [ConfigurationCollection(typeof(ExchangeConfiguration), AddItemName = "exchange")]
        public ExchangeConfigurationCollection ExchangeConfigs { get; set; }
    }

    public class BrokerConfigurationCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new BrokerConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BrokerConfiguration)element).Name;
        }
    }
}
