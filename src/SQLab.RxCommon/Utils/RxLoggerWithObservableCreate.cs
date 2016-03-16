using SqCommon;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace RxCommon
{

    public sealed class RxLoggerWithObservableCreate : IObservable<LogItem>    // Multithreaded Singleton design pattern, sealed to prevent derivation, which could add instances.
    {
        private static volatile RxLoggerWithObservableCreate instance;  // volatile to ensure that assignment to the instance variable completes before the instance variable can be accessed
        private static object syncRoot = new Object();

        private IObservable<LogItem> m_logDataSource = null;
        List<IObserver<LogItem>> m_observers = new List<IObserver<LogItem>>();

        ConsoleLogObserver m_consoleLogObserver;
        IDisposable m_consoleLogHandler;

        private RxLoggerWithObservableCreate()
        {
            m_logDataSource = Observable.Create<LogItem>(observer =>
            {
                m_observers.Add(observer);
                //var timer = new System‌.Threading.Timer(TimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                //timer‌.Enabled = true;
                //timer‌.Interval = 100;
                //timer‌.Elapsed += OnTimerElapsed;
                //timer‌.Start(‌);
                //return timer;

                //Action a = new Action(() =>
                //    timer.Dispose()
                //);
                //return a;
                return () => {      // this Action delegate will be called at Observable.Dispose(), to Unsubscribe from the sequence

                    m_observers.Remove(observer);
                    //timer.Dispose();
                };
            });

            m_consoleLogObserver = new ConsoleLogObserver();
            m_consoleLogHandler = Subscribe(m_consoleLogObserver);

        }

        //public  void TimerCallback(object state)
        //{

        //}

        public static RxLoggerWithObservableCreate Instance
        {
            get
            {
                // Double-Check Locking [Lea99] idiom to keep separate threads from creating new instances of the singleton at the same time.
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new RxLoggerWithObservableCreate();
                    }
                }

                return instance;
            }
        }

        public IDisposable Subscribe(IObserver<LogItem> p_observer)
        {
            //return m_logDataSource.Subscribe(p_observer);     // not sync Observation
            //return m_logDataSource.SubscribeOn(NewThreadScheduler.Default).Subscribe(p_observer);   // this way the Subscribe is called on a separate thread later. But I want the subscription right now, instanteniously.
            //return m_logDataSource.ObserveOn(NewThreadScheduler.Default).Subscribe(p_observer);   // this way the Subscribe is called in current thread, but later OnNext()s will be called in separate thread. Cool.
            return m_logDataSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(p_observer);   // for tasks less than 50msec, don't create new Thread, use TaskPool, the advanced ThreadPool
        }

        public void Info(string p_message)
        {
            var logItem = new LogItem() { CallerThreadId = Thread.CurrentThread.ManagedThreadId, LogLevel = LogLevel.Info, Timestamp = DateTime.UtcNow, Message = p_message };
            foreach (var observer in m_observers)
            {
                observer.OnNext(logItem);
            }
        }
    }
}
