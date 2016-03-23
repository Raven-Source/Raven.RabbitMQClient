using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    /// <summary>
    /// 生产者配置
    /// </summary>
    public class ProducerConfiguration : ConfigurationElement
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
        /// 确认超时时间，单位毫秒，默认100
        /// </summary>
        [ConfigurationProperty("sendConfirmTimeout", DefaultValue = 100)]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
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
        /// <summary>
        /// 后台发送消息线程数，默认1
        /// </summary>
        [ConfigurationProperty("maxWorker", DefaultValue = 1)]
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
    }
}
