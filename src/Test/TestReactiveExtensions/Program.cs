﻿using System;
using System.Reactive.Disposables;
using System.Linq;
using System.Reactive.Linq;

namespace TestReactiveExtensions
{
   


    public class Program
    {
        public static void Main(string[] args)
        {
            //IObservable<int> source = Observable.Empty<int>();
            //IObservable<int> source = Observable.Throw<int>(new Exception("Oops"));
            //IObservable<int> source = Observable.Return(42);
            //IObservable<int> source = Observable.Range(5, 3);

            // GenerateWithTime is now just an overload of Generate.
            // Observable.GenerateWithTime have disappeared in RX 2.0, but the PDF hand-on labs is not updated
            //IObservable<int> source = Observable.FromEvent()
            
            //.Throttle(TimeSpan.FromSeconds(1))  // if new messages comes within 1 sec, it resets the timer!!!
            IObservable<int> source = Observable.Generate(0, 
                i => i < 5, 
                i => i + 1, 
                i => i * i);

            var source2 = source.Timestamp()
                .Do(inp => Console.WriteLine("Before finishing" + inp.Timestamp.Millisecond + " " + inp.Value))
                .Select(r => r.Value);    // it is the RX printf() debugging

            using (source2.Subscribe(
                x => Console.WriteLine("OnNext: {0}", x),
                ex => Console.WriteLine("OnError: {0}", ex.Message),
                () => Console.WriteLine("OnCompleted")))
            {
                Console.WriteLine("Press ENTER to unsubscribe...");
                Console.ReadLine();
            }



            //Console.ReadKey();
        }
    }
}
