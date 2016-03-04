using SQCommon;
using System;
using System.Threading;

namespace RxCommon
{
    public class ConsoleLogObserver : IObserver<LogItem>
    {
        public void OnNext(LogItem p_logItem)
        {
            // a bit shorter format than going to file
            if (p_logItem.LogLevel >= LogLevel.Warn)    // Console shows only important messages, Warn, Error, Fatal
                Console.WriteLine($"{p_logItem.Timestamp.ToString("dd'T'HH':'mm':'ss.fff")}#{p_logItem.CallerThreadId}#{Thread.CurrentThread.ManagedThreadId}#{p_logItem.LogLevel}: {p_logItem.Message}");
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
