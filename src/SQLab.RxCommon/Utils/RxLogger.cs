using SqCommon;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace RxCommon
{
    // The Intro to Rx book says: "Subjects provide a convenient way to poke around Rx, however they are not recommended for day to day use.
    // An explanation is in the Usage Guidelines in the appendix. Instead of using subjects, favor the factory methods we will look at in Part 2.
    //  >We also have seen our first factory method in Subject.Create().
    // Others: Observable.Create()"

    // in Usage guidelines:
    // - Avoid the use of the subject types. Rx is effectively a functional programming paradigm. Using subjects means we are now managing state, 
    // (George: because subject becomes a variable, on which we can later call OnNexts() in an imperative way. With the Observable.Create() we don't store that variable into state later, because there is no point. Only Subscribe can be called on it. But that is not enough for a Logger.)
    // which is potentially mutating. Dealing with both mutating state and asynchronous programming at the same time is very hard to get right. 
    // Furthermore, many of the operators (extension methods) have been carefully written to ensure that correct and consistent lifetime of subscriptions and sequences is maintained; 
    // when you introduce subjects, you can break this. Future releases may also see significant performance degradation if you explicitly use subjects.
    // -Avoid creating your own implementations of the IObservable<T> interface. Favor using the Observable.Create factory method overloads instead.

    // George: Not convincing explanations. 
    // - the Subject version of Logger has much shorter code.
    // - with Subject, we don't have to implement List<IObserver<string>> m_observers; administration with concurrency locks. That would be a headache and error prone. Coders of Subjects already implemented it properly. Tested.
    // - his reasoning doesn't apply: any Logger is already a Mutating State. Logger is a side effect. If I implement it with Observable.Create() it has state too.
    // - End result with Subject: Amazing!: just in 5-10 lines of code, there is a multi-threaded ConsoleLogger, where logging happens Async

    public class LogItem
    {
        public DateTime Timestamp { get; set; }
        public int CallerThreadId { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
    }

    public sealed class RxLogger : ILogger   // Multithreaded Singleton design pattern, sealed to prevent derivation, which could add instances.
    {
        private static volatile RxLogger instance;  // volatile to ensure that assignment to the instance variable completes before the instance variable can be accessed
        private static object syncRoot = new Object();

        private Subject<LogItem> m_logDataSubject = null;
        List<IDisposable> m_observerHandles = new List<IDisposable>();
        
        private RxLogger()
        {
            m_logDataSubject = new Subject<LogItem>();

            //m_consoleLogHandler = m_logDataSubject.Subscribe(m_consoleLogObserver);   // not async, not multi-threaded Observation, Rx is not multi-threaded by default
            //m_consoleLogHandler = m_logDataSubject.SubscribeOn(NewThreadScheduler.Default).Subscribe(m_consoleLogObserver); // this way the Subscribe is called on a separate thread later. But I want the subscription right now, instanteniously.
            //m_consoleLogHandler = m_logDataSubject.ObserveOn(TaskPoolScheduler.Default).Subscribe(m_consoleLogObserver);    // this way the Subscribe is called in current thread, but later OnNext()s will be called in separate thread. Cool.
            //m_consoleLogHandler = m_logDataSubject.ObserveOn(TaskPoolScheduler.Default).Subscribe(new ConsoleLogObserver());    // for tasks less than 50msec, don't create new Thread, use TaskPool, the advanced ThreadPool
        }

        public static RxLogger Instance
        {
            get
            {
                // Double-Check Locking [Lea99] idiom to keep separate threads from creating new instances of the singleton at the same time.
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new RxLogger();
                    }
                }

                return instance;
            }
        }

        public void SubscribeObserver(IObserver<LogItem> p_observer)
        {
            IDisposable handler = m_logDataSubject.ObserveOn(TaskPoolScheduler.Default).Subscribe(p_observer);  // for tasks less than 50msec, don't create new Thread, use TaskPool, the advanced ThreadPool
            lock (m_observerHandles)
                m_observerHandles.Add(handler);    
        }

        public void Trace(string p_message)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Trace, Timestamp = DateTime.UtcNow, Message = p_message });
        }

        public void Debug(string p_message)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Debug, Timestamp = DateTime.UtcNow, Message = p_message });
        }

        public void Info(string p_message)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Info, Timestamp = DateTime.UtcNow, Message = p_message });
        }

        public void Info(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file.
            string str = $"{p_message} : EXCEPTION - INFO: {p_ex.Message}";
            Console.WriteLine(str);
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Info, Timestamp = DateTime.UtcNow, Message = str });
        }

        public void Warn(string p_message)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Warn, Timestamp = DateTime.UtcNow, Message = p_message });
        }

        public void Warn(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file. Warn level logs are listened by the ConsoleObserver
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Warn, Timestamp = DateTime.UtcNow, Message = $"{p_message} : EXCEPTION - INFO: {p_ex.Message}" });
        }

        public void Warn(string p_fmt, params object[] p_args)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Warn, Timestamp = DateTime.UtcNow, Message = String.Format(p_fmt, p_args) });
        }

        public void Error(string p_message)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Error, Timestamp = DateTime.UtcNow, Message = p_message });
        }

        public void Error(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file. Warn level logs are listened by the ConsoleObserver
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Error, Timestamp = DateTime.UtcNow, Message = $"{p_message} : EXCEPTION - INFO: {p_ex.Message}" });
        }

        public void Error(string p_fmt, params object[] p_args)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Error, Timestamp = DateTime.UtcNow, Message = String.Format(p_fmt, p_args) });
        }

        public void Fatal(string p_message)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Fatal, Timestamp = DateTime.UtcNow, Message = p_message });
        }

        public void Fatal(Exception p_ex, string p_message)
        {
            // this was an Exception. That is important. Write it to the Console too, not only the log file. Warn level logs are listened by the ConsoleObserver
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Fatal, Timestamp = DateTime.UtcNow, Message = $"{p_message} : EXCEPTION - INFO: {p_ex.Message}" });
        }

        public void Fatal(string p_fmt, params object[] p_args)
        {
            m_logDataSubject.OnNext(new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Fatal, Timestamp = DateTime.UtcNow, Message = String.Format(p_fmt, p_args) });
        }


        public void Exit()
        {
            m_logDataSubject.OnCompleted();     // signal observers completion. File can be closed
        }
        
    }
}
