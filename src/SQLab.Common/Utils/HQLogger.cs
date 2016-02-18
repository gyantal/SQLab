using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SQCommon
{
    // - this is temporary, until nLog is fixed
    // - m_t.Wait(); is not enough, because 2 parallel threads can pass the Wait() at the same time, and try to do m_file.WriteLineAsync() at the same time which is not allowed
    // - Exception: The stream is currently in use by a previous operation on the stream.,     at System.IO.StreamWriter.CheckAsyncTaskInProgress()    at System.IO.StreamWriter.WriteLineAsync(String value)
    // - in the future, a queue and a separate thread loop could be implemented, but for now just allow access of 1 thread
    // - in the future, nLog will be used, so we don't have to implement anything
    public class SQLogger 
    {
        StreamWriter m_file;    // StreamWriter is a specialized TextWriter
        int m_nLogsToFile = 0;
        Task m_t = null;
        public SQLogger(string p_filePath)
        {
            FileStream fs = new FileStream(p_filePath, FileMode.Append);
            m_file = new StreamWriter(fs);
            m_file.AutoFlush = true;    // auto-Flush anych is ok. so no manual flush is required
        }

        public void Debug(string p_message)
        {
            lock (m_file)
            {
                if (m_t != null)
                    m_t.Wait();
                if (m_file == null)
                    return;
                m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": DEBUG: " + p_message);
                m_nLogsToFile++;
            }
        }

        public void Info(string p_message)
        {
            lock (m_file)
            {
                if (m_t != null)
                    m_t.Wait();
                if (m_file == null)
                    return;
                m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": " + p_message);
                m_nLogsToFile++;
            }
        }

        public void Info(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file
            Console.WriteLine(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": EXCEPTION-INFO: " + p_ex.Message + ", Msg: " + p_message);

            lock (m_file)
            {
                if (m_t != null)
                    m_t.Wait();
                if (m_file == null)
                    return;
                m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": EXCEPTION-INFO: " + p_ex.Message + ", Msg: " + p_message);
                m_nLogsToFile++;
            }
        }

        public void Error(string p_message)
        {
            FormattedError(Utils.FormatMessageWithTimestamp("ERROR: {0}", p_message));
        }

        public void Error(string p_fmt, params object[] p_args)
        {
            FormattedError(Utils.FormatMessageWithTimestamp("ERROR: {0}", Utils.FormatInvCult(p_fmt, p_args)));
            //if (Level >= TraceLevel.Error)
            //    OnFormattedMsg(TraceLevel.Error, FormatMessage(p_fmt, p_args));
        }

        public void FormattedError(string p_formattedMessage)
        {
            lock (m_file)
            {
                if (m_t != null)
                    m_t.Wait();
                if (m_file == null)
                    return;
                m_t = m_file.WriteLineAsync(p_formattedMessage);
                m_nLogsToFile++;
            }
        }


        public void Flush()
        {
            lock (m_file)
            {
                if (m_t != null)
                    m_t.Wait();
                if (m_file == null)
                    return;
                m_file.Flush();
            }
        }

        public void FlushAsync()
        {
            lock (m_file)
            {
                if (m_t != null)
                    m_t.Wait();
                if (m_file == null)
                    return;
                m_t = m_file.FlushAsync();
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
