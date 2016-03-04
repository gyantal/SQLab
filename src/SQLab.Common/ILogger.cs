using System;

namespace SQCommon
{
    public interface ILogger
    {
        void Trace(string p_message);
        void Debug(string p_message);
        void Info(string p_message);
        void Info(Exception p_ex, string p_message);
        void Error(string p_message);
        void Error(string p_fmt, params object[] p_args);
        void Exit();
    }
}