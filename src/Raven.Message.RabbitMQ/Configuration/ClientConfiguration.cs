using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    public class ClientConfiguration : ConfigurationSection
    {
        static ClientConfiguration _instance = null;
        public static ClientConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ConfigurationManager.GetSection("ravenRabbitMQ") as ClientConfiguration;
                }
                return _instance;
            }
        }

        /// <summary>
        /// 日志实现类
        /// </summary>
        [ConfigurationProperty("logType", IsRequired = true)]
        public string LogType
        {
            get
            {
                return (string)this["logType"];
            }
            set
            {
                this["logType"] = value;
            }
        }
        /// <summary>
        /// 序列化类型
        /// </summary>
        [ConfigurationProperty("serializerType", IsRequired = true)]
        public SerializerType SerializerType
        {
            get
            {
                return (SerializerType)this["serializerType"];
            }
            set
            {
                this["serializerType"] = value;
            }
        }

        [ConfigurationProperty("brokers")]
        [ConfigurationCollection(typeof(BrokerConfiguration),AddItemName ="broker")]
        public BrokerConfigurationCollection Brokers
        {
            get
            {
                return (BrokerConfigurationCollection)this["brokers"];
            }
            set
            {
                this["brokers"] = value;
            }
        }
    }
}
