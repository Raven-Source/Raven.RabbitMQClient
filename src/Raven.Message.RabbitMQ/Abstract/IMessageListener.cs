//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Raven.Message.RabbitMQ.Abstract
//{
//    /// <summary>
//    /// 消息监听者，实现者需要线程安全
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    public interface IMessageListener<T>
//    {
//        /// <summary>
//        /// 消息接收自动完成
//        /// </summary>
//        bool AutoComplete { get; set; }
//        /// <summary>
//        /// 当消息收到时触发
//        /// </summary>
//        /// <param name="message"></param>
//        void OnMessageReceive(T message);
//    }
//}
