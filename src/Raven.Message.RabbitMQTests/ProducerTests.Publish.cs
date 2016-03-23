using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Tests
{
    public partial class ProducerTests
    {
        const string ExistExchange = "exist";
        const string NotExistExchange = "notexist";
        const string ConflictExchange = "conflict";

        /// <summary>
        /// 路由器存在，定义不冲突
        /// </summary>
        [TestMethod]
        public void PublishTest()
        {
            DeclareExchange(ExistExchange);
            string queue = BindQueue(ExistExchange);
            string message = "PublishTest";
            bool success = _client.Producer.Publish<string>(message, ExistExchange);
            Assert.IsTrue(success);
            AssertMessageReceived(queue, message);
            DeleteQueue(queue);
        }
        /// <summary>
        /// 路由器不存在
        /// </summary>
        [TestMethod]
        public void PublishTest_NotExist()
        {
            DeleteExchange(NotExistExchange);
            string message = "PublishTest_NotExist";
            bool success = _client.Producer.Publish<string>(message, NotExistExchange);
            Assert.IsTrue(success);
        }
        /// <summary>
        /// 路由器冲突
        /// </summary>
        [TestMethod]
        public void PublishTest_Conflict()
        {
            DeclareExchange(ConflictExchange);
            string queue = BindQueue(ConflictExchange);
            string message = "PublishTest_Conflict";
            bool success = _client.Producer.Publish<string>(message, ConflictExchange);
            Assert.IsFalse(success);
            AssertMessageNotReceived(queue);
            DeleteQueue(queue);
        }
        /// <summary>
        /// 路由器存在，定义不冲突
        /// </summary>
        [TestMethod]
        public void PublishToBuffTest()
        {
            DeclareExchange(ExistExchange);
            string queue = BindQueue(ExistExchange);
            string message = "PublishTest";
            _client.Producer.PublishToBuff<string>(message, ExistExchange);
            Thread.Sleep(500);
            AssertMessageReceived(queue, message);
            DeleteQueue(queue);
        }
        /// <summary>
        /// 路由器不存在
        /// </summary>
        [TestMethod]
        public void PublishToBuffTest_NotExist()
        {
            DeleteExchange(NotExistExchange);
            string message = "PublishTest_NotExist";
            _client.Producer.PublishToBuff<string>(message, NotExistExchange);
        }
        /// <summary>
        /// 路由器冲突
        /// </summary>
        [TestMethod]
        public void PublishToBuffTest_Conflict()
        {
            DeclareExchange(ConflictExchange);
            string queue = BindQueue(ConflictExchange);
            string message = "PublishTest_Conflict";
            _client.Producer.PublishToBuff<string>(message, ConflictExchange);
            Thread.Sleep(500);
            AssertMessageNotReceived(queue);
            DeleteQueue(queue);
        }
        private void DeclareExchange(string exchange)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.ExchangeDeclareNoWait(exchange, "topic", false, false, null);
            }
        }

        private static void DeleteExchange(string exchange)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                channel.ExchangeDeleteNoWait(exchange, false);
            }
        }

        private string BindQueue(string exchange)
        {
            using (IModel channel = _client.Connection.CreateModel())
            {
                var r = channel.QueueDeclare();
                channel.QueueBind(r.QueueName, exchange, "#");
                return r.QueueName;
            }
        }
    }
}
