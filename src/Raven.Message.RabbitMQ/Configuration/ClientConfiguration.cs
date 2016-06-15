using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

        public static ClientConfiguration LoadFrom(string file, string section)
        {
            if (!File.Exists(file))
                return null;
            var fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = file;
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            var configSource = config.GetSection(section) as ClientConfiguration;
            _instance = configSource;
            return _instance;
        }

        public ClientConfiguration()
        {
            SerializerType = SerializerType.MsgPack;
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
        /// 序列化类型，默认为MsgPack
        /// </summary>
        [ConfigurationProperty("serializerType", DefaultValue = SerializerType.MsgPack)]
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
        /// <summary>
        /// 服务端配置
        /// </summary>
        [ConfigurationProperty("brokers", IsRequired = true)]
        [ConfigurationCollection(typeof(BrokerConfiguration), AddItemName = "broker")]
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
