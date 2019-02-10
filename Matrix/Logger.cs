using Microsoft.Extensions.Logging;

namespace Matrix
{
    public static class Logger
    {
        public static ILoggerFactory Factory = LoggerFactory.Create(builder =>
        {
            
        });
    }
}