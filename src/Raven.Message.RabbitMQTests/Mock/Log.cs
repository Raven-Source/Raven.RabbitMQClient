using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQTests.Mock
{
    public class Log : ILog
    {
        static List<string> infos = new List<string>();
        static List<string> errors = new List<string>();

        public void LogDebug(string info, object dataObj)
        {
            infos.Add(info);
        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            errors.Add(errorMessage);
        }

        public static bool ContainsInfo(string info)
        {
            return infos.Contains(info);
        }
    }
}
