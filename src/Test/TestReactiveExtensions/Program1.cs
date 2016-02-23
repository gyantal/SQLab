using System;
using System.Reactive.Disposables;

namespace TestReactiveExtensions1
{
    public class MySequenceOfNumbersStream : IObservable<int>
    {
        public IDisposable Subscribe(IObserver<int> observer)
        {
            observer.OnNext(1);
            observer.OnNext(2);
            observer.OnNext(3);
            observer.OnCompleted();
            return Disposable.Empty;        // System.Reactive.Disposables;
        }
    }

    public class MyConsoleObserver<T> : IObserver<T>
    {
        public void OnNext(T value)
        {
            Console.WriteLine("Received value {0}", value);
        }
        public void OnError(Exception error)
        {
            Console.WriteLine("Sequence faulted with {0}", error);
        }
        public void OnCompleted()
        {
            Console.WriteLine("Sequence terminated");
        }
    }



    public class Program
    {
        // Don't leave as Main(), because 'dotnet compile':  "Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point."
        public static void Main1(string[] args)
        {
            var numbersStream = new MySequenceOfNumbersStream();
            var observer = new MyConsoleObserver<int>();
            numbersStream.Subscribe(observer);
        }
    }
}
