using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Abstract
{
    /// <summary>
    /// 节点观察者
    /// </summary>
    public interface IBrokerWatcher
    {
        /// <summary>
        /// 获取节点连接字符串
        /// </summary>
        /// <param name="brokerName">节点名</param>
        /// <returns>连接字符串，格式 amqp://user:pass@hostName:port/vhost</returns>
        string GetBrokerUri(string brokerName);
        /// <summary>
        /// 节点连接变更事件
        /// </summary>
        event EventHandler<BrokerChangeEventArg> BrokerUriChanged;
    }

    public class BrokerChangeEventArg : EventArgs
    {
        /// <summary>
        /// 节点名
        /// </summary>
        public string BrokerName { get; set; }
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string BrokerUri { get; set; }
    }
}
