using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using Raven.Message.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Tests
{
    [TestClass()]
    public class ConsumerTests
    {
        static string ReceiveAndCompleteQueue = "ReceiveAndCompleteQueue";
        static string ReceiveListQueue = "ReceiveListQueue";
        static string OnReceiveQueue = "OnReceiveQueue";
        
        static Client _client = null;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Client.Init();
            _client = Client.GetInstance("localhost");
            DeleteQueue(ReceiveAndCompleteQueue);
            DeleteQueue(ReceiveListQueue);
            DeleteQueue(OnReceiveQueue);
        }

        [ClassCleanup]
        public static void Dispose()
        {
            Client.Dispose();
        }


        [TestMethod()]
        public void ReceiveAndCompleteTest()
        {
            string message = "ReceiveAndCompleteTest";
            DeclareQueue(ReceiveAndCompleteQueue);
            SendMessage(ReceiveAndCompleteQueue, message);
            string received = _client.Consumer.ReceiveAndComplete<string>(ReceiveAndCompleteQueue);
            Assert.AreEqual(message, received, false);
        }
        [TestMethod()]
        public void ReceiveListTest()
        {
            string message1 = "ReceiveListTest1";
            string message2 = "ReceiveListTest2";
            string message3 = "ReceiveListTest3";
            string message4 = "ReceiveListTest4";
            DeclareQueue(ReceiveListQueue);
            SendMessage(ReceiveListQueue, message1);
            SendMessage(ReceiveListQueue, message2);
            SendMessage(ReceiveListQueue, message3);
            SendMessage(ReceiveListQueue, message4);

            List<string> received = _client.Consumer.ReceiveAndComplete<string>(ReceiveListQueue, 5);
            Assert.IsTrue(received.Count == 4);
            Assert.AreEqual(message1, received[0], false);
            Assert.AreEqual(message2, received[1], false);
            Assert.AreEqual(message3, received[2], false);
            Assert.AreEqual(message4, received[3], false);
        }
        [TestMethod()]
        public void OnReceiveTest()
        {
            object o = new object();
            string message = "OnReceiveTest";
            DeclareQueue(OnReceiveQueue);
            bool success = _client.Consumer.OnReceive<string>(ReceiveAndCompleteQueue, (m, messageKey, messageId, correlationId, args) =>
            {
                Assert.AreEqual(message, m);
                Monitor.PulseAll(o);
                return true;
            });
            Assert.IsTrue(success);
            SendMessage(OnReceiveQueue, message);
            lock (o)
            {
                Monitor.Wait(o, 2000);
            }
            AssertMessageNotReceived(OnReceiveQueue);
        }

        private void SendMessage(string queue, string message)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: SerializerService.Serialize<string>(message, Serializer.SerializerType.MsgPack));
            }
        }

        private void DeclareQueue(string queue)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.QueueDeclareNoWait(queue, false, false, false, null);
            }
        }

        public static void DeleteQueue(string queue)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.QueueDeleteNoWait(queue, false, false);
            }
        }
        private void AssertMessageNotReceived(string queue)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                BasicGetResult getResult = channel.BasicGet(queue, true);
                Assert.IsNull(getResult);
            }
        }
    }
}