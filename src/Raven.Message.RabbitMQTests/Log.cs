using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQTests
{
    public class Log : ILog
    {
        public void LogDebug(string info, object dataObj)
        {
            Console.WriteLine("[DEBUG] {0}", info);
            Console.WriteLine(dataObj);
            //throw new NotImplementedException();
        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            Console.WriteLine("[ERROR] {0}", errorMessage);
            Console.WriteLine(dataObj);
            Console.WriteLine(ex);
            //throw new NotImplementedException();
        }
    }
}
