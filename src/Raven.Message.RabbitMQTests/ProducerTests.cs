using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
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
    public partial class ProducerTests
    {
        const string NotExistQueue = "notexist";
        const string ExistQueue = "exist";
        const string ConflictQueue = "conflict";
        const string RedeclareQueue = "redeclare";
        const string PlaceHolderQueue = "placeholder_{_IP}_{_ProcessId}";
        const string DelayQueue = "DelayQueue";
        static Client _client = null;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Client.Init();
            _client = Client.GetInstance("localhost");
            DeleteQueue(NotExistQueue);
            DeleteQueue(ExistQueue);
            DeleteQueue(ConflictQueue);
            DeleteQueue(RedeclareQueue);
            DeleteQueue(DelayQueue);
            DeleteExchange(NotExistExchange);
            DeleteExchange(ExistExchange);
            DeleteExchange(ConflictExchange);
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
        /// <summary>
        /// 队列名占位符测试
        /// </summary>
        [TestMethod()]
        public void SendToPlaceHolderQueueTest()
        {
            string message = "SendToPlaceHolderQueueTest";
            bool success = _client.Producer.Send<string>(message, PlaceHolderQueue, null);
            Assert.IsTrue(success);
            string queue = PlaceHolderQueue.Replace("{_IP}", RuntimeEnviroment.IP).Replace("{_ProcessId}", RuntimeEnviroment.ProcessId);
            AssertMessageReceived(queue, message);
            DeleteQueue(queue);
        }
        /// <summary>
        /// 延迟消息测试
        /// </summary>
        [TestMethod()]
        public void SendDelayMessageTest()
        {
            string message = DateTime.Now.ToString();
            bool success = _client.Producer.SendDelay<string>(message, DelayQueue);
            Assert.IsTrue(success);
            Thread.Sleep(500);
            AssertMessageNotReceived(DelayQueue);
            Thread.Sleep(510);
            AssertMessageReceived(DelayQueue, message);
        }

        public static void DeleteQueue(string queue)
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