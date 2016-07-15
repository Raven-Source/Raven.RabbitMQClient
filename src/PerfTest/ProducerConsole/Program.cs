using Raven.Message.RabbitMQ;
using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProducerConsole
{
    class Program
    {
        static Client _client = null;
        static long _sendCount = 0;
        static long _sendFailed = 0;
        static long _sendElapsedMilliseconds = 0;

        static long _lastSend = 0;
        static long _lastSendFailed = 0;
        static long _lastSendElapsedMilliseconds = 0;

        static string _action = ConfigurationManager.AppSettings["action"];
        static int _cpu = int.Parse(ConfigurationManager.AppSettings["cpu"]);
        static int _batchCount = int.Parse(ConfigurationManager.AppSettings["batchCount"]);
        static int _sleepTime = int.Parse(ConfigurationManager.AppSettings["sleepTime"]);
        static string _message = ConfigurationManager.AppSettings["message"];

        //static string _message = "{\"tid\":\"123123123\",\"name\":\"陈女士\",\"card\":\"2016040500011215\",\"score\":100,\"scoreChange\":200,\"scoreChangeTime\":\"2016-04-10 15:30:12\",\"scoreChangeTime\":\"2016-04-10 15:30:12\",\"scoreChangeTime\":\"2016-04-10 15:30:12\",\"failedDes\":\"重复积分\",\"scoreType\":\"消费积分\",\"scoreChangeTime\":\"2016-04-10 15:30:12\",\"scoreChangeTime\":\"2016-04-10 15:30:12\",\"userType\":\"乐享卡\",\"mobile\":\"13562151024\",\"level\":\"金卡\",\"oldLevel\":\"银卡\",\"score\":500,\"rightDes\":\"消费满300免费停车2小时\"}";



        static void Main(string[] args)
        {
            Client.Init();
            _client = Client.GetInstance("perftest");

            List<Task> _tasks = new List<Task>();
            for (int i = 0; i < _cpu * 1; i++)
            {
                _tasks.Add(Task.Factory.StartNew(Run));
            }
            Console.WriteLine("all thread started");

            while (true)
            {
                Thread.Sleep(10000);
                PrintStats();
            }
        }

        static void PrintStats()
        {
            long sendCount = _sendCount;
            long sendFailed = _sendFailed;
            long sendElapsedMilliseconds = _sendElapsedMilliseconds;
            Console.WriteLine($"{DateTime.Now}, totalSend : {sendCount}, sendFailed : {sendFailed}, elapsedMilliseconds : {sendElapsedMilliseconds}");
            Console.WriteLine($"lastSend : {sendCount - _lastSend}, lastSendFailed : {sendFailed - _lastSendFailed}, lastElapsedMilliseconds : {sendElapsedMilliseconds - _lastSendElapsedMilliseconds}");
            _lastSend = sendCount;
            _lastSendFailed = sendFailed;
            _lastSendElapsedMilliseconds = sendElapsedMilliseconds;
        }

        static void Run()
        {
            Stopwatch stopwatch = new Stopwatch();
            Func<bool> func = SendToQueue;
            switch (_action)
            {
                case "SendToQueueAsync":
                    func = SendToQueueAsync;
                    break;
                case "SendToQueue":
                    func = SendToQueue;
                    break;
                case "SendToQueueWithPriority":
                    func = SendToQueueWithPriority;
                    break;
                case "Publish":
                    func = Publish;
                    break;
            }
            while (true)
            {
                int count = _batchCount;
                int failed = 0;
                stopwatch.Start();
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        bool success = func();
                        if (!success)
                            failed++;
                    }
                    catch
                    {
                        failed++;
                    }
                }
                stopwatch.Stop();
                Interlocked.Add(ref _sendCount, count);
                Interlocked.Add(ref _sendFailed, failed);
                Interlocked.Add(ref _sendElapsedMilliseconds, stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();
                Thread.Sleep(_sleepTime);
            }
        }


        static bool SendToQueueAsync()
        {
            _client.Producer.SendToBuff<string>(_message, "testqueue");
            return true;
        }

        static bool SendToQueue()
        {
            return _client.Producer.Send<string>(_message, "testqueue");
        }

        static int _priorityCount = 0;
        static bool SendToQueueWithPriority()
        {
            SendOption option = new SendOption();
            option.Priority = (byte)(_priorityCount % 5);
            _priorityCount++;
            return _client.Producer.Send<string>(_message, "testqueue", option);
        }

        static bool Publish()
        {
            return _client.Producer.Publish<string>(_message, "perfexchange", "test");
        }
    }

    public class Log : ILog
    {
        public void LogDebug(string info, object dataObj)
        {

        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "producer.log"), string.Format("{0} {1} {2} {3}{4}", DateTime.Now, errorMessage, ex, dataObj, Environment.NewLine));
        }
    }
}
