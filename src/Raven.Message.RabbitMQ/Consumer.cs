using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 消费者
    /// </summary>
    public class Consumer
    {
        internal BrokerConfiguration BrokerConfig { get; set; }

        internal FacilityManager Facility { get; set; }

        internal ILog Log { get; set; }

        internal ChannelManager Channel { get; set; }

        internal Consumer()
        {

        }

        public T Receive<T>(string queue, out ulong tag, string messageKey = null)
        {
            tag = 0;
            throw new NotImplementedException();
        }

        public void Complete(ulong tag)
        {
            throw new NotImplementedException();
        }

        public T ReceiveAndComplete<T>(string queue, string messageKey = null)
        {
            throw new NotImplementedException();
        }

        public void OnReceive<T>(Action<T> callback, string queue, bool autoComplete, string messageKey = null)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(Action<T> callback, string exchange, bool autoComplete, string queue = null, string messageKey = null)
        {
            throw new NotImplementedException();
        }
    }
}
