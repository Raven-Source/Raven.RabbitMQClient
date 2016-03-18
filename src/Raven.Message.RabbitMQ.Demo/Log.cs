using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Demo
{
    public class Log : Abstract.ILog
    {
        public void LogDebug(string info, object dataObj)
        {
            Console.WriteLine("DEBUG:{0}", info);
            Console.WriteLine(dataObj);
        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            Console.WriteLine("ERROR:{0}",errorMessage);
            Console.WriteLine(dataObj);
            Console.WriteLine(ex);
        }
    }
}
