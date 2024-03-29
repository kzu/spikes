﻿using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace TracingApp
{
    [EventSource(Name = "TracingApp-EventSourced")]
    class ComponentEvents : EventSource
    {
        /// <summary>
        /// The singleton instance of this event source.
        /// </summary>
        internal static readonly ComponentEvents Instance = new ComponentEvents();

        [Event(Events.Tick, Level = EventLevel.Informational, Message = "Elapsed {0} seconds", Opcode = EventOpcode.Info)]
        public void Tick(int elapsed) => WriteEvent(Events.Tick, elapsed);
    }

    public static class Events
    {
        public const int Tick = 1;
    }

    class Component : MarshalByRefObject
    {
        readonly DiagnosticSource source = new DiagnosticListener("TracingApp-DiagnosticSourced");
        readonly TraceSource tracer;

        public Component()
        {
            tracer = new TraceSource("TracingApp-TraceSourced");
            tracer.Listeners.Add(new DiagnosticSourceTraceListener());
        }

        public IDisposable Start()
        {
            var cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                var watch = Stopwatch.StartNew();
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000, cts.Token);

                    // EventSource
                    if (ComponentEvents.Instance.IsEnabled())
                        ComponentEvents.Instance.Tick(watch.Elapsed.Seconds);
                    
                    // DiagnosticSource
                    if (source.IsEnabled(nameof(Events.Tick), watch.Elapsed.Seconds))
                        source.Write(nameof(Events.Tick), new { Elapsed = watch.Elapsed.Seconds, Time = DateTime.Now });
                    
                    // TraceSource
                    if (tracer.Switch.ShouldTrace(TraceEventType.Information))
                        tracer.TraceEvent(TraceEventType.Information, Events.Tick, "Elapsed {0} seconds", watch.Elapsed.Seconds);
                }
            });

            return new Disposable(cts);
        }

        class Disposable : MarshalByRefObject, IDisposable
        {
            readonly CancellationTokenSource cancellation;

            public Disposable(CancellationTokenSource cancellation) => this.cancellation = cancellation;

            public void Dispose() => cancellation.Cancel();
        }
    }
}
