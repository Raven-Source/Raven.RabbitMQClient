using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Message.RabbitMQ;
using Raven.Message.RabbitMQ.Configuration;
using Raven.Message.RabbitMQTests.Mock;
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
            ClientConfiguration config = CreateClientConfig();

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
            //Client.Dispose();
        }

        [TestMethod()]
        public void ReloadTest_Subscribe()
        {
            ClientConfiguration config = CreateClientConfig();

            string exchange = "reloadtestexchange";
            string queue = "reloadtestexqueue";
            string pattern = "hello";
            string message = "hello";
            bool receivedAfterReload = false;

            Client.Init(config);
            bool success = Client.GetInstance("test").Consumer.Subscribe<string>(exchange, queue, pattern, (m, messageKey, messageId, correlationId, args) =>
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
            //Client.Dispose();
        }

        [TestMethod]
        public void InitTest_BrokerWatcher()
        {
            BrokerWatcher watcher = new BrokerWatcher();
            ClientConfiguration config = CreateClientConfig();
            config.LogType = "Raven.Message.RabbitMQTests.Mock.Log, Raven.Message.RabbitMQTests";
            Client.Init(config, watcher);

            string queue = "reloadtestqueue";
            string message = "hello";
            bool receivedAfterReload = false;
            bool success = Client.GetInstance("test").Consumer.OnReceive<string>(queue, (m, messageKey, messageId, correlationId, args) =>
            {
                receivedAfterReload = true;
                return true;
            });

            watcher.ChangeUri("test", "amqp://guest:1234qwer@121.40.119.230:5672");
            //GC.Collect();
            //Thread.Sleep(61000);
            //GC.Collect();
            //Assert.IsTrue(Log.ContainsInfo("client disposed by destructor"));
            Client.GetInstance("test").Producer.Send(message, queue);
            Thread.Sleep(500);
            Assert.IsTrue(receivedAfterReload);
            //Client.Dispose();
        }

        [TestMethod]
        public void OldClientGCTest()
        {
            BrokerWatcher watcher = new BrokerWatcher();
            ClientConfiguration config = CreateClientConfig();
            config.LogType = "Raven.Message.RabbitMQTests.Mock.Log, Raven.Message.RabbitMQTests";
            Client.Init(config, watcher);
            Client oldClient = Client.GetInstance("test");

            string queue = "oldClientGCTest";
            string message = "hello";
            bool receivedAfterReload = false;
            oldClient.Consumer.OnReceive<string>(queue, (m, messageKey, messageId, correlationId, args) =>
            {
                receivedAfterReload = true;
                return true;
            });

            watcher.ChangeUri("test", "amqp://guest:1234qwer@121.40.119.230:5672");

            oldClient.Producer.Send(message, queue);
            Thread.Sleep(500);
            Assert.IsTrue(receivedAfterReload);

            oldClient = null;
            Thread.Sleep(1000);
            GC.Collect();
            Thread.Sleep(1000);
            Assert.IsFalse(Log.ContainsInfo("client disposed by destructor"), "not disposed");

            Thread.Sleep(120000);
            GC.Collect();
            Assert.IsTrue(Log.ContainsInfo("client PrepareToDispose"), "prepare to dispose");
            Thread.Sleep(1000);
            Assert.IsTrue(Log.ContainsInfo("client disposed by destructor"), "disposed");
        }

        private ClientConfiguration CreateClientConfig()
        {
            ClientConfiguration config = new ClientConfiguration();
            config.LogType = "Raven.Message.RabbitMQTests.Log,Raven.Message.RabbitMQTests";
            config.Brokers = new BrokerConfigurationCollection();

            BrokerConfiguration brokerConfig = new BrokerConfiguration();
            config.Brokers.Add(brokerConfig);

            brokerConfig.Name = "test";
            brokerConfig.Uri = "amqp://127.0.0.1";
            return config;
        }
    }
}