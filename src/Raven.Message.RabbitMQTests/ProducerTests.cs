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
    public partial class ProducerTests
    {
        const string NotExistQueue = "notexist";
        const string ExistQueue = "exist";
        const string ConflictQueue = "conflict";
        const string RedeclareQueue = "redeclare";
        static Client _client = null;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Client.Init();
            _client = Client.GetInstance("localhost");
        }

        [ClassCleanup]
        public static void Dispose()
        {
            Client.Dispose();
        }

        /// <summary>
        /// 队列不存在
        /// </summary>
        [TestMethod()]
        public void SendTest_QueueNotExist()
        {
            DeleteQueue(NotExistQueue);
            string message = "SendTest_QueueNotExist";
            bool success = _client.Producer.Send<string>(message, NotExistQueue, null);
            Assert.IsTrue(success);
            AssertMessageReceived(NotExistQueue, message);
        }
        /// <summary>
        /// 队列已存在，定义不冲突
        /// </summary>
        [TestMethod()]
        public void SendTest()
        {
            DeclareQueue(ExistQueue);
            string message = "SendTest";
            bool success = _client.Producer.Send<string>(message, ExistQueue, null);
            Assert.IsTrue(success);
            AssertMessageReceived(ExistQueue, message);
        }
        /// <summary>
        /// 队列已存在，定义冲突
        /// </summary>
        [TestMethod()]
        public void SendTest_QueueConflict()
        {
            DeclareQueue(ConflictQueue);
            string message = "SendTest_QueueConflict";
            bool success = _client.Producer.Send<string>(message, ConflictQueue, null);
            Assert.IsFalse(success);
            AssertMessageNotReceived(ConflictQueue);
        }
        /// <summary>
        /// 队列已存在，定义冲突，重新定义队列
        /// </summary>
        [TestMethod()]
        public void SendTest_ReDeclareQueue()
        {
            DeleteQueue(RedeclareQueue);
            DeclareQueue(RedeclareQueue);
            string message = "SendTest_ReDeclareQueue";
            bool success = _client.Producer.Send<string>(message, RedeclareQueue, null);
            Assert.IsTrue(success);
            AssertMessageReceived(RedeclareQueue, message);
        }
        /// <summary>
        /// 队列不存在
        /// </summary>
        [TestMethod()]
        public void SendToBuffTest_QueueNotExist()
        {
            DeleteQueue(NotExistQueue);
            string message = "SendToBuffTest_QueueNotExist";
            _client.Producer.SendToBuff<string>(message, NotExistQueue, null);
            Thread.Sleep(500);
            AssertMessageReceived(NotExistQueue, message);
        }
        /// <summary>
        /// 队列已存在，定义不冲突
        /// </summary>
        [TestMethod()]
        public void SendToBuffTest()
        {
            DeclareQueue(ExistQueue);
            string message = "SendTest";
            _client.Producer.SendToBuff<string>(message, ExistQueue, null);
            Thread.Sleep(500);
            AssertMessageReceived(ExistQueue, message);
        }
        /// <summary>
        /// 队列已存在，定义冲突
        /// </summary>
        [TestMethod()]
        public void SendToBuffTest_QueueConflict()
        {
            DeclareQueue(ConflictQueue);
            string message = "SendTest_QueueConflict";
            _client.Producer.SendToBuff<string>(message, ConflictQueue, null);
            Thread.Sleep(500);
            AssertMessageNotReceived(ConflictQueue);
        }
        /// <summary>
        /// 队列已存在，定义冲突，重新定义队列
        /// </summary>
        [TestMethod()]
        public void SendToBuffTest_ReDeclareQueue()
        {
            DeleteQueue(RedeclareQueue);
            DeclareQueue(RedeclareQueue);
            string message = "SendTest_ReDeclareQueue";
            _client.Producer.SendToBuff<string>(message, RedeclareQueue, null);
            Thread.Sleep(500);
            AssertMessageReceived(RedeclareQueue, message);
        }

        private void DeleteQueue(string queue)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.QueueDeleteNoWait(queue, false, false);
            }
        }

        private void AssertMessageReceived(string queue,string message)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                BasicGetResult getResult = channel.BasicGet(queue, true);
                string received = SerializerService.Deserialize<string>(getResult?.Body, Serializer.SerializerType.MsgPack);
                Assert.AreEqual<string>(message, received);
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

        private void DeclareQueue(string queue)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.QueueDeclareNoWait(queue, false, false, false, null);
            }
        }
    }
}