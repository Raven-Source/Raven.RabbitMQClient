using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    public class DeclareConfiguration : ConfigurationElement
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
        private string _storageName;
        /// <summary>
        /// 存储名
        /// </summary>
        public string StorageName
        {
            get
            {
                if (string.IsNullOrEmpty(_storageName))
                {
                    _storageName = Name.Replace("{_IP}", RuntimeEnviroment.IP).Replace("{_ProcessId}", RuntimeEnviroment.ProcessId);
                }
                return _storageName;
            }
        }
        /// <summary>
        /// 当没有消费者时，是否自动删除，默认不删除
        /// </summary>
        [ConfigurationProperty("autoDelete", DefaultValue = false)]
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
        /// 可重用，在消息中间件重启后是否还能继续使用
        /// </summary>
        [ConfigurationProperty("durable", DefaultValue = false)]
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
    }
}
