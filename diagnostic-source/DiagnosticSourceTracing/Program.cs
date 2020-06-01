using System;
using System.Diagnostics;
using System.Threading;

namespace DiagnosticSourceTracing
{
    class Program
    {
        static readonly TraceSource trace = new TraceSource("LegacyTraceSource");

        static void Main(string[] args)
        {
            // NOTE: some platform/generic code would set up this listener and 
            // switch on the logging on-demand, most likely.
            trace.Listeners.Add(new DiagnosticSourceTraceListener());
            // NOTE: we turn on *all* logging because it's the listener the one 
            // doing the checks instead, based on listeners subscribed to the diagnostic source.
            trace.Switch.Level = SourceLevels.All;

            int count = 0;
            while (true)
            {
                trace.TraceEvent(TraceEventType.Information, 1, $"Processing {count}");
                trace.TraceData(TraceEventType.Verbose, 2, count);
                Console.WriteLine($"Processing {count}...");
                Thread.Sleep(1000);
                count++;
            }
        }
    }
}
