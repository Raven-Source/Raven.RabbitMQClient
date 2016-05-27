using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Demo
{
    public class Log : Abstract.ILog
    {
        static string _log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mq.log");
        public void LogDebug(string info, object dataObj)
        {
            File.AppendAllText(_log, string.Format("DEBUG:{0}", info));
            if (dataObj != null)
            {
                File.AppendAllText(_log, dataObj.ToString());
            }
            Console.WriteLine("DEBUG:{0}", info);
            Console.WriteLine(dataObj);
        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            File.AppendAllText(_log, string.Format("ERROR:{0}", errorMessage));
            if (dataObj != null)
            {
                File.AppendAllText(_log, dataObj.ToString());
            }
            if (ex != null)
            {
                File.AppendAllText(_log, ex.ToString());
            }
            Console.WriteLine("ERROR:{0}", errorMessage);
            Console.WriteLine(dataObj);
            Console.WriteLine(ex);
        }
    }
}
