using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace TracingApp
{
    class Program
    {
        static readonly string LogFile = Path.GetTempFileName();

        static void Main(string[] args)
        {
            List<DiagnosticListener> allListeners = new List<DiagnosticListener>();
            DiagnosticListener.AllListeners.Subscribe(d => allListeners.Add(d));

            using var listener = new EventListener();
            listener.EventSourceCreated += (sender, args) => File.AppendAllText(LogFile, $"EventSourceCreated: {args.EventSource.Name}" + Environment.NewLine);
            listener.EventWritten += OnEventWritten;

            var tracer = new TraceSource("TracingApp-TraceSourced");
            tracer.Listeners.Add(new DiagnosticSourceTraceListener());

            using var disposable = new Component(tracer).Start();

            Console.WriteLine("Usage: [+|0][source]");
            Console.WriteLine("Examples:");
            Console.WriteLine("  +TracingApp-EventSourced         Enables the EventSource-based events");
            Console.WriteLine("  +TracingApp-DiagnosticSourced    Enables the DiagnosticSource-based events");
            Console.WriteLine("  +TracingApp-TraceSourced         Enables the TraceSource-based events");

            Console.WriteLine("  -TracingApp-EventSourced         Disables the EventSource-based events");
            Console.WriteLine("  -TracingApp-DiagnosticSourced    Disables the DiagnosticSource-based events");
            Console.WriteLine("  -TracingApp-TraceSourced         Disables the TraceSource-based events");

            Console.WriteLine("Opening log file " + LogFile);
            Process.Start(new ProcessStartInfo("code", LogFile) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });

            var allSources = EventSource.GetSources().ToDictionary(source => source.Name);
            allSources.TryGetValue("Microsoft-Diagnostics-DiagnosticSource", out EventSource? diagSource);

            var line = Console.ReadLine();
            while (line.Length > 0)
            {
                var sourceName = line.Substring(1);
                allSources.TryGetValue(sourceName, out EventSource? evtSource);

                switch (line[0])
                {
                    case '+':
                        if (evtSource != null)
                        {
                            listener.EnableEvents(evtSource, EventLevel.Informational);
                            Console.WriteLine($"Enabled {sourceName} EventSource");
                        }

                        // For DiagnosticSource, we use the bridge:
                        if (diagSource != null && 
                            (allListeners.Any(d => d.Name == sourceName) || sourceName == tracer.Name))
                        {
                            listener.EnableEvents(diagSource, EventLevel.Informational, EventKeywords.All, new Dictionary<string, string>
                            {
                                { "FilterAndPayloadSpecs", sourceName }
                            });
                            Console.WriteLine($"Enabled {sourceName} via {diagSource.Name}");
                        }

                        if (sourceName == tracer.Name)
                            tracer.Switch.Level = SourceLevels.Information;

                        break;
                    case '-':
                        if (evtSource != null)
                            listener.DisableEvents(evtSource);

                        // NOTE: this will disable it too for other events that are not the one we filtered for
                        // so unsubscription needs to be more selective (or we need to re-enable the ones we added 
                        // before again.
                        if (diagSource != null &&
                            (allListeners.Any(d => d.Name == sourceName) || sourceName == tracer.Name))
                        {
                            listener.DisableEvents(diagSource);
                        }

                        if (sourceName == tracer.Name)
                            tracer.Switch.Level = SourceLevels.Off;

                        break;
                    default:
                        break;
                }

                line = Console.ReadLine();
            }
        }

        static void OnEventWritten(object sender, EventWrittenEventArgs args)
        {
            // Payload has specific shape for this source
            if (args.EventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
            {
                // For the DiagnosticSource bridge, we only care about Event events, which are the ones that 
                // the app has written to the source.
                if (args.EventId != 2)
                    return;

                var arguments = string.Join(", ",
                    ((object[])args.Payload[2]).OfType<IDictionary<string, object>>()
                        .Select(dict => dict["Key"] + "=" + dict["Value"]));

                // The EventId isn't used at all in the DiagnosticSource API
                WriteLog($"{args.Payload[0]}: {args.Payload[1]}:{arguments}");
            }
            else
            {
                var message = args.Message;
                if (!string.IsNullOrEmpty(message) && args.Payload.Count > 0)
                    message = string.Format(message, args.Payload.ToArray());

                WriteLog($"{args.EventSource.Name}: {args.EventName}:{args.EventId}:{message}");
            }
        }

        static void WriteLog(string message)
        {
            if (!message.EndsWith(Environment.NewLine))
                message += Environment.NewLine;

            while (true)
            {
                try
                {
                    File.AppendAllText(LogFile, message);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
