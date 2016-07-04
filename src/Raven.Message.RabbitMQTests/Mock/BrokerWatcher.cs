using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQTests.Mock
{
    public class BrokerWatcher : IBrokerWatcher
    {
        public event EventHandler<BrokerChangeEventArg> BrokerUriChanged;

        public string GetBrokerUri(string brokerName)
        {
            return "amqp://127.0.0.1";
        }

        public void ChangeUri(string brokerName, string uri)
        {
            BrokerUriChanged(this, new BrokerChangeEventArg() { BrokerName = brokerName, BrokerUri = uri });
        }
    }
}
