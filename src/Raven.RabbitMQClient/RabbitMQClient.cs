using RabbitMQ.Client;
using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.MessageQueue.WithRabbitMQ
{
    public class RabbitMQClient
    {
        private IDataSerializer serializer;

        private ILoger loger;
        private static readonly object objLock = new object();
        private IConnection _connection;
        private ConnectionFactory factory;
        private IConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    lock (objLock)
                    {
                        if (_connection == null)
                        {
                            _connection = CreateConnection();
                        }
                    }
                }
                return _connection;
            }
        }

        /// <summary>
        /// 队列工作线程
        /// </summary>
        private Thread queueWorkThread;
        private bool isQueueToWrite;
        private AutoResetEvent resetEvent;

        private System.Collections.Concurrent.ConcurrentQueue<QueueMessage> _queue;
        private int _maxQueueCount;
        private int waitMillisecondsTimeout;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="userName"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        /// <param name="maxQueueCount"></param>
        /// <param name="serializerType"></param>
        /// <param name="loger"></param>
        private RabbitMQClient(string hostName, string userName, string password, int? port, int maxQueueCount
            , SerializerType serializerType, ILoger loger = null)
        {
            factory = new ConnectionFactory();
            factory.HostName = hostName;
            if (!port.HasValue || port.Value < 0)
            {
                factory.Port = 5672;
            }
            else
            {
                factory.Port = port.Value;
            }
            factory.Password = password;
            factory.UserName = userName;

            serializer = SerializerFactory.Create(serializerType);
            _queue = new System.Collections.Concurrent.ConcurrentQueue<QueueMessage>();
            _maxQueueCount = maxQueueCount > 0 ? maxQueueCount : Options.DefaultMaxQueueCount;

            isQueueToWrite = false;
            resetEvent = new AutoResetEvent(false);

            this.loger = loger;
            this.waitMillisecondsTimeout = 30000;

            queueWorkThread = new Thread(QueueToWrite);
            queueWorkThread.IsBackground = true;
            queueWorkThread.Start();
        }

        /// <summary>
        /// 获取实例
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static RabbitMQClient GetInstance(Options options)
        {
            return new RabbitMQClient(options.HostName, options.UserName, options.Password, options.Port, options.MaxQueueCount
                , options.SerializerType, options.Loger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IConnection CreateConnection()
        {
            return factory.CreateConnection();
        }

        /// <summary>
        /// 出对
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName">队列名</param>
        /// <param name="isDurable">是否持久化队列</param>
        /// <returns></returns>
        public List<T> Dequeue<T>(string queueName, bool isDurable = false)
        {
            List<T> list = new List<T>();
            try
            {
                using (IModel channel = Connection.CreateModel())
                {
                    channel.QueueDeclare(queueName, isDurable, false, false, null);
                    while (true)
                    {
                        BasicGetResult res = channel.BasicGet(queueName, false);
                        if (res != null)
                        {
                            channel.BasicAck(res.DeliveryTag, false);
                            try
                            {
                                T obj = serializer.Deserialize<T>(res.Body);
                                list.Add(obj);
                            }
                            catch (Exception ex)
                            {
                                RecordException(ex);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _connection.Close(100);
                    _connection.Dispose();
                }
                catch { }
                _connection = null;
                RecordException(ex);
            }
            return list;
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="dataObj"></param>
        /// <param name="exchangeType">默认为空，值为fanout是支持订阅发布模型</param>
        public void Enqueue<T>(string queueName, T dataObj, ExchangeType exchangeType = ExchangeType.Direct)
        {
            string strExchangeType = ExchangeTypeDataDict.ExchangeTypeDict[exchangeType];

            string exchangeName = string.Empty;
            try
            {
                using (IModel channel = Connection.CreateModel())
                {
                    if (exchangeType == ExchangeType.Fanout)
                    {
                        exchangeName = "publish";
                        channel.ExchangeDeclare(exchangeName, strExchangeType);
                    }
                    channel.QueueDeclare(queueName, true, false, false, null);
                    byte[] data = serializer.Serialize(dataObj);
                    channel.BasicPublish(exchangeName, queueName, null, data);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _connection.Close(100);
                    _connection.Dispose();
                }
                catch { }
                _connection = null;
                RecordException(ex);
                //throw;
            }
        }

        /// <summary>
        /// 是否异步调用入队的方法
        /// </summary>
        /// <typeparam name="T">入队的数据类型</typeparam>
        /// <param name="queueName">队列名</param>
        /// <param name="dataObj">入队数据</param>
        /// <param name="exchangeType">通道</param>
        public void EnqueueAysnc<T>(string queueName, T dataObj, ExchangeType exchangeType = ExchangeType.Direct)
        {
            if (_queue.Count < _maxQueueCount)
            {
                var qm = new QueueMessage();
                qm.queueName = queueName;
                qm.data = dataObj;
                qm.exchangeType = exchangeType;

                _queue.Enqueue(qm);

                resetEvent.Set();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void QueueToWrite()
        {
            while (true)
            {
                resetEvent.Reset();
                if (_queue.Count > 0)
                {
                    isQueueToWrite = true;
                    QueueMessage qm = null;
                    while (_queue.TryDequeue(out qm))
                    {
                        try
                        {
                            Enqueue<object>(qm.queueName, qm.data, qm.exchangeType);
                        }
                        catch (Exception ex)
                        {
                            RecordException(ex);
                        }

                        SpinWait.SpinUntil(() => false, 1);
                    }

                    isQueueToWrite = false;
                }

                resetEvent.WaitOne(waitMillisecondsTimeout);
                //SpinWait.SpinUntil(() => false, waitMillisecondsTimeout);
                //Thread.Sleep(waitMillisecondsTimeout);
            }
        }


        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="ex"></param>
        private void RecordException(Exception ex)
        {
            if (loger != null)
            {
                loger.LogError(ex.Message, ex);
            }
        }

    }


    /// <summary>
    /// 队列消息
    /// </summary>
    public class QueueMessage
    {
        public string queueName;
        public object data;
        public ExchangeType exchangeType;
    }
}
