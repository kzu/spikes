﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace TracingApp
{
    partial class Program
    {
        static bool opened;
        static readonly string LogFile = Path.GetTempFileName();

        static void Main(string[] args)
        {
            // The in-proc version of the forwarder
            var localForwarder = new EventForwarder();
            localForwarder.EventWritten += (s, e) => WriteEvent(e);

            var domain = AppDomain.CreateDomain("ComponentDomain");
            var remote = (Component)domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Component).FullName);
            var local = new Component();

            var remoteForwarder = (EventForwarder)domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(EventForwarder).FullName);
            remoteForwarder.EventWritten += (s, e) => WriteEvent(e);


            using var remoteDisposable = remote.Start();
            //using var localDisposable = local.Start();

            Console.WriteLine("Usage: [+|0][source]");
            Console.WriteLine("Examples:");
            Console.WriteLine("  +TracingApp-EventSourced         Enables the EventSource-based events");
            Console.WriteLine("  +TracingApp-DiagnosticSourced    Enables the DiagnosticSource-based events");
            Console.WriteLine("  +TracingApp-TraceSourced         Enables the TraceSource-based events");

            Console.WriteLine("  -TracingApp-EventSourced         Disables the EventSource-based events");
            Console.WriteLine("  -TracingApp-DiagnosticSourced    Disables the DiagnosticSource-based events");
            Console.WriteLine("  -TracingApp-TraceSourced         Disables the TraceSource-based events");

            var line = Console.ReadLine();
            while (line.Length > 0)
            {
                var sourceName = line.Substring(1);

                switch (line[0])
                {
                    case '+':
                        localForwarder.Enable(sourceName);
                        remoteForwarder.Enable(sourceName);
                        break;
                    case '-':
                        localForwarder.Disable(sourceName);
                        remoteForwarder.Disable(sourceName);
                        break;
                    default:
                        break;
                }

                line = Console.ReadLine();
            }
        }

        static void WriteEvent(string payload)
        {
            // NOTE: deserializing directly to the original event args is not really 
            // possible, since it doesn't have a public constructor, it receives an EventSource, etc.
            //OnEventWritten(JsonConvert.DeserializeObject<EventWrittenEventArgs>(payload, settings)!);

            OnEventWritten(JsonConvert.DeserializeObject<EventWrittenInfo>(payload));
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

            if (!opened)
            {
                opened = true;
                Console.WriteLine("Opening log file " + LogFile);
                Process.Start(new ProcessStartInfo("code", LogFile) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
            }
        }

        static void OnEventWritten(EventWrittenInfo args)
        {
            // Payload has specific shape for this source
            if (args.EventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
            {
                // For the DiagnosticSource bridge, we only care about Event events, which are the ones that 
                // the app has written to the source.
                if (args.EventId != 2)
                    return;

                var arguments = string.Join(", ",
                    ((IEnumerable<object>)args.Payload[2]).OfType<IDictionary<string, object>>()
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
    }
}
