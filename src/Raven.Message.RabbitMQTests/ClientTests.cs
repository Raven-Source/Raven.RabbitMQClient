using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Message.RabbitMQ;
using Raven.Message.RabbitMQ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Tests
{
    [TestClass()]
    public class ClientTests
    {
        [TestMethod()]
        public void ReloadTest()
        {
            ClientConfiguration config = new ClientConfiguration();
            config.LogType = "Raven.Message.RabbitMQTests.Log,Raven.Message.RabbitMQTests";
            config.Brokers = new BrokerConfigurationCollection();

            BrokerConfiguration brokerConfig = new BrokerConfiguration();
            config.Brokers.Add(brokerConfig);

            brokerConfig.Name = "test";
            brokerConfig.Uri = "amqp://127.0.0.1";

            string queue = "reloadtestqueue";
            string message = "hello";
            bool receivedAfterReload = false;

            Client.Init(config);
            bool success = Client.GetInstance("test").Consumer.OnReceive<string>(queue, (m, messageKey, messageId, correlationId, args) =>
            {
                receivedAfterReload = true;
                return true;
            });

            config.Brokers = new BrokerConfigurationCollection();
            BrokerConfiguration brokerConfig1 = new BrokerConfiguration();
            config.Brokers.Add(brokerConfig1);
            brokerConfig1.Name = "test";
            brokerConfig1.Uri = "amqp://guest:1234qwer@121.40.119.230:5672";
            Client.Reload(config);

            Client.GetInstance("test").Producer.Send(message, queue);
            Thread.Sleep(500);
            Assert.IsTrue(receivedAfterReload);
            
        }

        [TestMethod()]
        public void ReloadTest_Subscribe()
        {
            ClientConfiguration config = new ClientConfiguration();
            config.LogType = "Raven.Message.RabbitMQTests.Log,Raven.Message.RabbitMQTests";
            config.Brokers = new BrokerConfigurationCollection();

            BrokerConfiguration brokerConfig = new BrokerConfiguration();
            config.Brokers.Add(brokerConfig);

            brokerConfig.Name = "test";
            brokerConfig.Uri = "amqp://127.0.0.1";

            string exchange = "reloadtestexchange";
            string queue = "reloadtestexqueue";
            string pattern = "hello";
            string message = "hello";
            bool receivedAfterReload = false;

            Client.Init(config);
            bool success = Client.GetInstance("test").Consumer.Subscribe<string>(exchange, queue, pattern,(m, messageKey, messageId, correlationId, args) =>
            {
                receivedAfterReload = true;
                return true;
            });

            config.Brokers = new BrokerConfigurationCollection();
            BrokerConfiguration brokerConfig1 = new BrokerConfiguration();
            config.Brokers.Add(brokerConfig1);
            brokerConfig1.Name = "test";
            brokerConfig1.Uri = "amqp://guest:1234qwer@121.40.119.230:5672";
            Client.Reload(config);

            Client.GetInstance("test").Producer.Publish(message, exchange, pattern);
            Thread.Sleep(500);
            Assert.IsTrue(receivedAfterReload);
        }
    }
}