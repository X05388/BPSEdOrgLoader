using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    public class EmailException : Exception
    {
        private static readonly ILog exceptionLogger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public EmailException(string message, Exception innerException) : base(message, innerException)
        {
            message = $"{innerException.GetType()?.Name} -- {message}";
            exceptionLogger.Error(message);
        }

        public EmailException(string message) : base(message)
        {
            
            exceptionLogger.Info(message);
        }
    }
}
