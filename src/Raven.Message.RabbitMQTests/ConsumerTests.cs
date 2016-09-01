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
        static string NewObjectQueue = "NewObjectQueue";
        static string OldObjectQueue = "OldObjectQueue";
        static string ErrorMessageQueue = "ErrorMessageQueue";

        static Client _client = null;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Client.Init();
            _client = Client.GetInstance("localhost");
            DeleteQueue(ReceiveAndCompleteQueue);
            DeleteQueue(ReceiveListQueue);
            DeleteQueue(OnReceiveQueue);
            DeleteQueue(NewObjectQueue);
            DeleteQueue(OldObjectQueue);
            DeleteQueue(ErrorMessageQueue);
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
            bool success = _client.Consumer.OnReceive<string>(OnReceiveQueue, (m, messageKey, messageId, correlationId, args) =>
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

        [TestMethod()]
        public void ReceiveNewObject()
        {
            DeclareQueue(NewObjectQueue);
            NewObject obj = new NewObject()
            {
                Des = "des",
                Id = "1111",
                Name = "hahaha"
            };
            SendObject(NewObjectQueue, obj);
            OldObject received = _client.Consumer.ReceiveAndComplete<OldObject>(NewObjectQueue);
            Assert.IsNotNull(received);
            Assert.AreEqual(obj.Name, received.Name);
        }

        [TestMethod()]
        public void ReceiveOldObject()
        {
            DeclareQueue(OldObjectQueue);
            OldObject obj = new OldObject()
            {
                Name = "hahaha"
            };
            SendObject(OldObjectQueue, obj);
            NewObject received = _client.Consumer.ReceiveAndComplete<NewObject>(OldObjectQueue);
            Assert.IsNotNull(received);
            Assert.AreEqual(obj.Name, received.Name);
        }

        [TestMethod()]
        public void ReceiveErrorMessage()
        {
            List<string> messages = new List<string>() { "1", "2", "3", "x", "4", "5", "6","7","8","9","10" };
            List<string> handled = new List<string>();
            DeclareQueue(ErrorMessageQueue);
            int tried = 0;
            Abstract.MessageReceived<string> callback = (m, messageKey, messageId, correlationId, args) =>
            {
                int i = 0;
                bool s = int.TryParse(m, out i);
                if (s)
                {
                    lock (handled)
                    {
                        handled.Add(m);
                    }
                }
                else
                {
                    tried++;
                }
                if (tried > 10)
                    return true;
                return s;
            };
            bool success = _client.Consumer.OnReceive<string>(ErrorMessageQueue, callback);
            success = _client.Consumer.OnReceive<string>(ErrorMessageQueue, callback);
            Assert.IsTrue(success);
            foreach (string message in messages)
            {
                SendObject(ErrorMessageQueue, message);
            }
            Thread.Sleep(1000);
            Assert.IsTrue(handled.Count == (messages.Count - 1));
        }

        private void SendMessage(string queue, string message)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: SerializerService.Serialize<string>(message, Serializer.SerializerType.MsgPack));
            }
        }

        private void SendObject(string queue, object obj)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: SerializerService.Serialize<object>(obj, Serializer.SerializerType.NewtonsoftJson));
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

        public class NewObject : OldObject
        {
            public string Id { get; set; }

            public string Des { get; set; }
        }

        public class OldObject
        {
            public string Name { get; set; }
        }
    }
}