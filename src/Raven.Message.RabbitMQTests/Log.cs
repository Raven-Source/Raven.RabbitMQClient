using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQTests
{
    public class Log : ILog
    {
        public void LogDebug(string info, object dataObj)
        {
            Debug.WriteLine("[DEBUG] {0}", info);
            Debug.WriteLine(dataObj);
        }

        public void LogError(string errorMessage, Exception ex, object dataObj)
        {
            Debug.WriteLine("[ERROR] {0}", errorMessage);
            Debug.WriteLine(dataObj);
            Debug.WriteLine(ex);
        }
    }
}
