using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SqCommon
{
    // - this is temporary, until nLog is fixed
    // - m_t.Wait(); is not enough, because 2 parallel threads can pass the Wait() at the same time, and try to do m_file.WriteLineAsync() at the same time which is not allowed
    // - Exception: The stream is currently in use by a previous operation on the stream.,     at System.IO.StreamWriter.CheckAsyncTaskInProgress()    at System.IO.StreamWriter.WriteLineAsync(String value)
    // - in the future, a queue and a separate thread loop could be implemented, but for now just allow access of 1 thread
    // - in the future, nLog will be used, so we don't have to implement anything

    public enum LogLevel { Trace, Debug, Info, Warn, Error, Fatal };  // from nLog https://github.com/NLog/NLog/wiki/Log-levels

    public class SQLogger : ILogger
    {
        StreamWriter m_file;    // StreamWriter is a specialized TextWriter
        Task m_t = null;

        public SQLogger(string p_filePath)
        {
            FileStream fs = new FileStream(p_filePath, FileMode.Append);
            m_file = new StreamWriter(fs);
            m_file.AutoFlush = true;    // auto-Flush asych is ok. so no manual flush is required
        }

        public void Trace(string p_message)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Trace}: {p_message}");
        }

        public void Debug(string p_message)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Debug}: {p_message}");
        }

        public void Info(string p_message)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Info}: {p_message}");
        }

        public void Info(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file
            string str = $"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Info}: {p_message}, : EXCEPTION: {p_ex.ToStringWithShortenedStackTrace(400)}";
            Console.WriteLine(str);
            WriteToFileVerbatim(str);
        }

        public void Warn(string p_message)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Warn}: {p_message}");
        }

        public void Warn(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file
            string str = $"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Warn}: {p_message}, : EXCEPTION: {p_ex.ToStringWithShortenedStackTrace(400)}";
            Console.WriteLine(str);
            WriteToFileVerbatim(str);
        }

        public void Warn(string p_fmt, params object[] p_args)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Warn}: {Utils.FormatInvCult(p_fmt, p_args)}");
            //WriteToFileVerbatim(Utils.FormatMessageWithTimestamp("ERROR: {0}", Utils.FormatInvCult(p_fmt, p_args)));
            //if (Level >= TraceLevel.Error)
            //    OnFormattedMsg(TraceLevel.Error, FormatMessage(p_fmt, p_args));
        }

        public void Error(string p_message)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Error}: {p_message}");
        }

        public void Error(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file
            string str = $"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Error}: {p_message}, : EXCEPTION: {p_ex.ToStringWithShortenedStackTrace(400)}";
            Console.WriteLine(str);
            WriteToFileVerbatim(str);
        }

        public void Error(string p_fmt, params object[] p_args)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Error}: {Utils.FormatInvCult(p_fmt, p_args)}");
            //WriteToFileVerbatim(Utils.FormatMessageWithTimestamp("ERROR: {0}", Utils.FormatInvCult(p_fmt, p_args)));
            //if (Level >= TraceLevel.Error)
            //    OnFormattedMsg(TraceLevel.Error, FormatMessage(p_fmt, p_args));
        }

        public void Fatal(string p_message)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Fatal}: {p_message}");
        }

        public void Fatal(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file
            string str = $"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Fatal}: {p_message}, : EXCEPTION: {p_ex.ToStringWithShortenedStackTrace(400)}";
            Console.WriteLine(str);
            WriteToFileVerbatim(str);
        }

        public void Fatal(string p_fmt, params object[] p_args)
        {
            WriteToFileVerbatim($"{DateTime.UtcNow.ToString("MMdd'T'HH':'mm':'ss.fff")}#{Thread.CurrentThread.ManagedThreadId}#{LogLevel.Fatal}: {Utils.FormatInvCult(p_fmt, p_args)}");
            //WriteToFileVerbatim(Utils.FormatMessageWithTimestamp("ERROR: {0}", Utils.FormatInvCult(p_fmt, p_args)));
            //if (Level >= TraceLevel.Error)
            //    OnFormattedMsg(TraceLevel.Error, FormatMessage(p_fmt, p_args));
        }

        private void WriteToFileVerbatim(string p_formattedMessage)
        {
            lock (m_file)
            {
                if (m_t != null)
                {
                    m_t.Wait();
                    //await m_t;  // An await expression cannot be used in the block of a lock statement.
                }
                if (m_file == null)
                    return;
                m_t = m_file.WriteLineAsync(p_formattedMessage);
            }
        }

        public void Exit()
        {
            lock (m_file)
            {
                StreamWriter grabFile = m_file;
                m_file = null;  // quickly replace it, so that other threads in Info() cannot use it any more

                if (m_t != null)
                    m_t.Wait();
                grabFile.Flush();
                grabFile.Dispose();
            }
        }

        
    }
}
