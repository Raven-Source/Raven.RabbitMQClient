using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.MessageQueue.WithRabbitMQ;
using Raven.Serializer;
using Raven.MessageQueue;
using Newtonsoft.Json;
using System.Threading;

namespace Raven.RabbitMQClient.TestConsole
{
    public class Loger : ILoger
    {
        public void LogError(Exception ex, object dataObj)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    public class Program
    {

        private static readonly string hostName = ConfigurationManager.AppSettings["RabbitMQHost"];
        private static readonly string username = "liangyi";
        private static readonly string password = "123456";

        public static MessageQueue.WithRabbitMQ.RabbitMQClient Instance = MessageQueue.WithRabbitMQ.RabbitMQClient.GetInstance(new Options()
        {
            SerializerType = SerializerType.NewtonsoftJson,
            HostName = hostName,
            Password = password,
            UserName = username,
            MaxQueueCount = 100000,
            Loger = new Loger()
        });

        static void Main(string[] args)
        {
            string key = null;

            do
            {
                Console.WriteLine("press a key");
                Console.WriteLine("1.enqueue");
                Console.WriteLine("2.dequeue");
                Console.WriteLine("3.publish");
                Console.WriteLine("4.subscribe");
                Console.WriteLine("5.RegisterReceive");
                Console.Write("-->");
                key = Console.ReadLine();
                var arr = key.Split(' ');

                switch (arr[0])
                {
                    case "1":
                        Enqueue();
                        break;
                    case "2":
                        Dequeue();
                        break;
                    case "3":
                        Publish();
                        break;
                    case "4":
                        //if (arr.Length > 1)
                        //{
                        //    Subscribe(arr[1]);
                        //}
                        Subscribe();
                        break;
                    case "5":
                        RegisterReceive();
                        break;
                }

            } while (key != "q");

        }

        static void Enqueue()
        {
            User obj = new User();
            obj.ID = 124;
            obj.Name = "dagds大公司gg";
            obj.Time = DateTime.Now;

            Instance.Send("exlog2", obj, false, true);
            Console.WriteLine("Enqueue end");
        }

        static void Publish()
        {
            User obj = new User();
            obj.ID = 5325;
            obj.Name = "dagds大公司gg";
            obj.Time = DateTime.Now;

            Instance.Publish("exlog2", obj);
        }


        static void Subscribe()
        {
            Instance.Subscribe<User>("exlog2", x =>
            {
                Console.WriteLine("Subscribe:{0}", JsonConvert.SerializeObject(x));
            });
        }

        static void Dequeue()
        {
            var userList = Instance.ReceiveBatch<User>("exlog2");

            foreach (var user in userList)
            {
                Console.WriteLine(JsonConvert.SerializeObject(user));
                //Console.WriteLine(user.Name);
                //Console.WriteLine(user.ID.ToString());
                //Console.WriteLine(user.Time.ToString());
            }
            Console.WriteLine("Dequeue end");
        }


        static void RegisterReceive()
        {
            Instance.RegisterReceive<User>("exlog2", x =>
            {
                Console.WriteLine("Receive:{0}", JsonConvert.SerializeObject(x));
                return true;
            }, noAck: true);
        }
    }

    public class User
    {
        public string Name;
        public int ID;
        public DateTime Time;

    }
}
