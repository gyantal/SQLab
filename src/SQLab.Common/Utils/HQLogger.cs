using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SQCommon
{
    // this is temporary, until nLog is fixed
    public class SQLogger
    {
        StreamWriter m_file;    // StreamWriter is a specialized TextWriter
        int m_nLogsToFile = 0;
        Task m_t = null;
        public SQLogger(string p_filePath)
        {
            FileStream fs = new FileStream(p_filePath, FileMode.Append);
            m_file = new StreamWriter(fs);
            m_file.AutoFlush = true;
        }

        public void Debug(string p_message)
        {
            if (m_t != null)
                m_t.Wait();
            if (m_file == null)
                return;
            m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": DEBUG: " + p_message);
            m_nLogsToFile++;
            //if (m_nLogsToFile % 2 == 0)
            //    FlushAsync();
        }

        public void Info(string p_message)
        {
            if (m_t != null)
                m_t.Wait();
            if (m_file == null)
                return;
            m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": " + p_message);
            m_nLogsToFile++;
            //if (m_nLogsToFile % 2 == 0)
            //    FlushAsync();
        }

        public void Info(Exception p_ex, string p_message)
        {
            if (m_t != null)
                m_t.Wait();
            if (m_file == null)
                return;
            m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ": EXCEPTION-INFO: " + p_ex.Message + ", Msg: " + p_message);
            m_nLogsToFile++;
            //if (m_nLogsToFile % 2 == 0)
            //    FlushAsync();
        }

        public void Error(string p_message)
        {
            if (m_t != null)
                m_t.Wait();
            if (m_file == null)
                return;
            m_t = m_file.WriteLineAsync(DateTime.UtcNow.ToString("MM'-'dd H:mm:ss", CultureInfo.InvariantCulture) + ":ERROR: " + p_message);
            m_nLogsToFile++;
            //if (m_nLogsToFile % 2 == 0)
            //    FlushAsync();
        }

        public void Flush()
        {
            if (m_t != null)
                m_t.Wait();
            if (m_file == null)
                return;
            m_file.Flush();
        }

        public void FlushAsync()
        {
            if (m_t != null)
                m_t.Wait();
            if (m_file == null)
                return;
            m_t = m_file.FlushAsync();
        }

        public void Exit()
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
