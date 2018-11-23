using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RxCommon
{
    // Decision to use file.WriteLine() instead of file.WriteLineAsynch(). (but it can be changed later) Reasons:
    // - shorter source code, cleaner, less error prone
    // - FileLogObserver Observation is on a separate thread already, so File.WriteAsync() is not really required. 
    // - FileLogObserver.Observation is happining on the TaskPool thread, not on a free created thread, and we are supposed to take the TaskPool thread only max. 50msec. Which is
    // OK, because writing 50bytes (one line) was 1.1msec in 1974, so it is much better now. Probably we can write to the file in 1msec all. 
    // Yes. Measuring it: File.WriteLine() took: 0.0068 msec, not even 1 msec. So, Writing file in another new thread is not necessary.
    // So, we don't take the TaskPool thread for more than 50msec
    // However, lock (m_file) is still needed, because two threads cannot Write() it at the same time
    public class FileLogObserver : IObserver<LogItem>
    {
        StreamWriter m_file;    // StreamWriter is a specialized TextWriter
        
        public FileLogObserver(string p_filePath)
        {
            FileStream fs = new FileStream(p_filePath, FileMode.Append);
            m_file = new StreamWriter(fs);
            m_file.AutoFlush = true;    // auto-Flush anych is ok. so no manual flush is required
        }

        public void OnNext(LogItem p_logItem)
        {
            // return IsShowingDatePart ? String.Format("{1:x}{0:dd}{2}{0:HH':'mm':'ss.fff}", p_timeUtc, p_timeUtc.Month, p_timeUtc.DayOfWeek.ToString().Substring(0, 2))
            //    : p_timeUtc.ToString("HH\\:mm\\:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            //String.Format("{0}#{1:d2} {2}", FormatNow(), Thread.CurrentThread.ManagedThreadId, Utils.FormatInvCult(p_fmt, p_args));
            //Console.WriteLine($"ThId-{Thread.CurrentThread.ManagedThreadId}: CallThId-{p_logItem.CallerThreadId}_{p_logItem.Timestamp}_{p_logItem.LogLevel}: {p_logItem.Message}");

            //WriteToFileVerbatim($"ThId-{Thread.CurrentThread.ManagedThreadId}: CallThId-{p_logItem.CallerThreadId}_{p_logItem.Timestamp}_{p_logItem.LogLevel}: {p_logItem.Message}");

            string log = $"{p_logItem.Timestamp.ToString("MMdd'T'HH':'mm':'ss.fff")}#{p_logItem.CallerThreadId}#{Thread.CurrentThread.ManagedThreadId}#{p_logItem.LogLevel}: {p_logItem.Message}";
            Debug.WriteLine(log);       // in DEBUG only: it sends logs to VS Output window too
            WriteToFileVerbatim(log);
        }

        private void WriteToFileVerbatim(string p_formattedMessage)
        {
            lock (m_file)
            {
                if (m_file == null)
                    return;
                m_file.WriteLine(p_formattedMessage);
            }
        }

        public void OnCompleted()
        {
            lock (m_file)
            {
                StreamWriter grabFile = m_file;
                m_file = null;  // quickly replace it, so that other threads in WriteToFileVerbatim() cannot use it any more

                grabFile.Flush();
                grabFile.Dispose();
            }
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
