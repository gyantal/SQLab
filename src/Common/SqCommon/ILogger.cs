using System;

namespace SqCommon
{
    public interface ILogger
    {
        void Trace(string p_message);
        void Debug(string p_message);
        void Info(string p_message);
        void Info(Exception p_ex, string p_message);
        void Warn(string p_message);
        void Warn(Exception p_ex, string p_message);
        void Warn(string p_fmt, params object[] p_args);
        void Error(string p_message);
        void Error(Exception p_ex, string p_message);
        void Error(string p_fmt, params object[] p_args);
        void Fatal(string p_message);
        void Fatal(Exception p_ex, string p_message);
        void Fatal(string p_fmt, params object[] p_args);
        void Exit();
    }
}