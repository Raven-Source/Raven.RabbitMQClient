using RabbitMQ.Client;
using Raven.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Events;

namespace Raven.MessageQueue.WithRabbitMQ
{
    /// <summary>
    /// 
    /// </summary>
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
                    try
                    {
                        Monitor.Enter(objLock);
                        if (_connection == null)
                        {
                            _connection = CreateConnection();
                        }
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        Monitor.Exit(objLock);
                    }

                    //lock (objLock)
                    //{
                    //    if (_connection == null)
                    //    {
                    //        _connection = CreateConnection();
                    //    }
                    //}
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

        private IBasicProperties propertiesEmpty = null;

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
            this.waitMillisecondsTimeout = 10000;

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
        /// 批量接收
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName">队列名</param>
        /// <returns></returns>
        public List<T> ReceiveBatch<T>(string queueName, string exchangeName = null)
        {
            List<T> list = new List<T>();
            List<BasicGetResult> resList = BasicDequeueBatch(exchangeName, queueName);
            foreach (var res in resList)
            {
                try
                {
                    T obj = serializer.Deserialize<T>(res.Body);
                    list.Add(obj);
                }
                catch (Exception ex)
                {
                    RecordException(ex, res.Body);
                }
            }

            return list;
        }

        /// <summary>
        /// 批量接收
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName">队列名</param>
        /// <returns></returns>
        public T ReceiveSingle<T>(string queueName, string exchangeName = null)
        {
            T model = default(T);
            BasicGetResult res = BasicDequeue(exchangeName, queueName);
            if (res != null)
            {
                try
                {
                    model = serializer.Deserialize<T>(res.Body);
                }
                catch (Exception ex)
                {
                    RecordException(ex, res.Body);
                }
            }
            return model;
        }

        /// <summary>
        /// 异步入队
        /// </summary>
        /// <param name="queueName">队列名</param>
        /// <param name="dataObj">入队数据</param>
        /// <param name="persistent">数据是否持久化</param>
        /// <param name="durableQueue">队列是否持久化</param>
        public void Send(string queueName, object dataObj, bool persistent = false, bool durableQueue = false)
        {
            if (_queue.Count < _maxQueueCount)
            {
                var qm = new QueueMessage();
                qm.exchangeName = "";
                qm.queueName = queueName;
                qm.data = dataObj;
                qm.persistent = persistent;
                qm.durableQueue = durableQueue;
                qm.exchangeType = ExchangeType.Default;

                _queue.Enqueue(qm);

                resetEvent.Set();
            }
        }

        public void Publish(string exchangeName, object dataObj)
        {
            if (_queue.Count < _maxQueueCount)
            {
                var qm = new QueueMessage();
                qm.exchangeName = exchangeName;
                qm.queueName = "";
                qm.data = dataObj;
                qm.persistent = false;
                qm.durableQueue = false;
                qm.exchangeType = ExchangeType.Fanout;

                _queue.Enqueue(qm);

                resetEvent.Set();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="callback"></param>
        public IModel Subscribe<T>(string exchangeName, Action<T> callback)
        {
            return BasicQueueBind(exchangeName, (o, res) =>
            {
                try
                {
                    var model = serializer.Deserialize<T>(res.Body);
                    if (callback != null)
                    {
                        callback(model);
                    }
                }
                catch (Exception ex)
                {
                    RecordException(ex, res.Body);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeType"></param>
        /// <returns></returns>
        private List<BasicGetResult> BasicDequeueBatch(string exchangeName, string queueName, ExchangeType exchangeType = ExchangeType.Default)
        {
            List<BasicGetResult> resList = new List<BasicGetResult>();
            try
            {
                using (IModel channel = Connection.CreateModel())
                {
                    if (exchangeType != ExchangeType.Default)
                    {
                        string strExchangeType = ExchangeTypeDataDict.ExchangeTypeDict[exchangeType];
                        channel.ExchangeDeclare(exchangeName, strExchangeType);
                    }
                    while (true)
                    {
                        BasicGetResult res = channel.BasicGet(queueName, false);
                        if (res != null)
                        {
                            resList.Add(res);
                            channel.BasicAck(res.DeliveryTag, false);
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
                if (!Monitor.IsEntered(objLock))
                {
                    if (_connection != null)
                    {
                        try
                        {
                            _connection.Close(100);
                            _connection.Dispose();
                        }
                        catch
                        {
                        }
                    }
                    _connection = null;
                }
                RecordException(ex, null);
            }

            return resList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeType"></param>
        /// <returns></returns>
        private BasicGetResult BasicDequeue(string exchangeName, string queueName, ExchangeType exchangeType = ExchangeType.Default)
        {
            BasicGetResult res = null;
            //List<BasicGetResult> resList = new List<BasicGetResult>();
            try
            {
                using (IModel channel = Connection.CreateModel())
                {
                    if (exchangeType != ExchangeType.Default)
                    {
                        string strExchangeType = ExchangeTypeDataDict.ExchangeTypeDict[exchangeType];
                        channel.ExchangeDeclare(exchangeName, strExchangeType);
                    }

                    res = channel.BasicGet(queueName, false);
                    if (res != null)
                    {
                        channel.BasicAck(res.DeliveryTag, false);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!Monitor.IsEntered(objLock))
                {
                    if (_connection != null)
                    {
                        try
                        {
                            _connection.Close(100);
                            _connection.Dispose();
                        }
                        catch
                        {
                        }
                    }
                    _connection = null;
                }
                RecordException(ex, null);
            }

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeType"></param>
        /// <returns></returns>
        private IModel BasicQueueBind(string exchangeName, EventHandler<BasicDeliverEventArgs> callback)
        {
            //List<BasicGetResult> resList = new List<BasicGetResult>();
            try
            {
                IModel channel = Connection.CreateModel();

                string strExchangeType = ExchangeTypeDataDict.ExchangeTypeDict[ExchangeType.Fanout];
                channel.ExchangeDeclare(exchangeName, strExchangeType);

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queueName, exchangeName, "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += callback;

                channel.BasicConsume(queueName, true, consumer);

                return channel;

                //res = channel.BasicGet(queueName, false);
                //if (res != null)
                //{
                //    channel.BasicAck(res.DeliveryTag, false);
                //}

            }
            catch (Exception ex)
            {
                if (!Monitor.IsEntered(objLock))
                {
                    if (_connection != null)
                    {
                        try
                        {
                            _connection.Close(100);
                            _connection.Dispose();
                        }
                        catch
                        {
                        }
                    }
                    _connection = null;
                }
                RecordException(ex, null);
            }

            return null;
        }


        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="dataObj"></param>
        /// <param name="persistent">数据是否持久化</param>
        /// <param name="durableQueue">队列是否持久化</param>
        /// <param name="exchangeType">默认为空，值为fanout是支持订阅发布模型</param>
        private bool BasicEnqueue(string exchangeName, string queueName, object dataObj, bool persistent = false, bool durableQueue = false, ExchangeType exchangeType = ExchangeType.Default)
        {
            try
            {
                using (IModel channel = Connection.CreateModel())
                {
                    if (exchangeType != ExchangeType.Default)
                    {
                        string strExchangeType = ExchangeTypeDataDict.ExchangeTypeDict[exchangeType];
                        channel.ExchangeDeclare(exchangeName, strExchangeType);
                    }
                    //if (exchangeType == ExchangeType.Fanout)
                    //{
                    //    exchangeName = "publish";
                    //}
                    if (durableQueue)
                    {
                        channel.QueueDeclare(queueName, true, false, false, null);
                    }

                    IBasicProperties properties = propertiesEmpty;
                    if (persistent)
                    {
                        properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                    }
                    byte[] data = serializer.Serialize(dataObj);
                    channel.BasicPublish(exchangeName, queueName, properties, data);

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (!Monitor.IsEntered(objLock))
                {
                    if (_connection != null)
                    {
                        try
                        {
                            _connection.Close(100);
                            _connection.Dispose();
                        }
                        catch
                        {
                        }
                    }
                    _connection = null;
                }
                RecordException(ex, dataObj);


                return false;
                //throw;
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
                    while (_queue.TryPeek(out qm))
                    {
                        if (BasicEnqueue(qm.exchangeName, qm.queueName, qm.data, qm.persistent, qm.durableQueue, qm.exchangeType))
                        {
                            _queue.TryDequeue(out qm);
                        }
                        else
                        {
                            break;
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
        /// <param name="dataObj"></param>
        private void RecordException(Exception ex, object dataObj)
        {
            if (loger != null)
            {
                loger.LogError(ex, dataObj);
            }
        }
    }

    /// <summary>
    /// 队列消息
    /// </summary>
    internal class QueueMessage
    {
        public string exchangeName;
        public string queueName;
        public object data;
        public bool persistent;
        public bool durableQueue;
        public ExchangeType exchangeType;
    }
}
