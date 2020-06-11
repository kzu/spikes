using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace TracingApp
{
    public class EventForwarder : MarshalByRefObject
    {
        static readonly List<WeakReference> traceSources = (List<WeakReference>)
            typeof(TraceSource).GetField("tracesources", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        public event EventHandler<string> EventWritten;

        readonly HashSet<string> enabledSources = new HashSet<string>();
        readonly List<DiagnosticListener> allListeners;
        readonly EventListener listener;

        public EventForwarder()
        {
            allListeners = new List<DiagnosticListener>();
            DiagnosticListener.AllListeners.Subscribe(d => allListeners.Add(d));

            listener = new EventListener();
            listener.EventWritten += OnEventWritten;
        }

        public void Enable(string source)
        {
            enabledSources.Add(source);

            var evtSource = EventSource.GetSources().FirstOrDefault(x => x.Name == source);
            var traceSource = traceSources.Where(x => x.IsAlive).Select(x => x.Target as TraceSource).FirstOrDefault(x => x?.Name == source);

            if (evtSource != null)
                listener.EnableEvents(evtSource, EventLevel.Informational);

            RefreshDiagnosticSource();

            if (traceSource != null)
                traceSource.Switch.Level = SourceLevels.Information;
        }

        void RefreshDiagnosticSource()
        {
            var diagSource = EventSource.GetSources().FirstOrDefault(x => x.Name == "Microsoft-Diagnostics-DiagnosticSource");
            if (diagSource == null)
                return;

            listener.DisableEvents(diagSource);
            if (enabledSources.Count > 0)
            {
                listener.EnableEvents(diagSource, EventLevel.Informational, EventKeywords.All, new Dictionary<string, string>
                {
                    { "FilterAndPayloadSpecs", string.Join(Environment.NewLine, enabledSources) }
                });
            }
        }

        public void Disable(string source)
        {
            enabledSources.Remove(source);

            var evtSource = EventSource.GetSources().FirstOrDefault(x => x.Name == source);
            var traceSource = traceSources.Where(x => x.IsAlive).Select(x => x.Target as TraceSource).FirstOrDefault(x => x?.Name == source);

            if (evtSource != null)
                listener.DisableEvents(evtSource);

            RefreshDiagnosticSource();

            if (traceSource != null)
                traceSource.Switch.Level = SourceLevels.Information;
        }

        void OnEventWritten(object sender, EventWrittenEventArgs args)
        {
            // Payload has specific shape for this source
            if (args.EventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
            {
                // For the DiagnosticSource bridge, we only care about Event events, which are the ones that 
                // the app has written to the source.
                if (args.EventId != 2)
                    return;

                EventWritten?.Invoke(this, JsonConvert.SerializeObject(args, Formatting.Indented));
                //var arguments = string.Join(", ",
                //    ((object[])args.Payload[2]).OfType<IDictionary<string, object>>()
                //        .Select(dict => dict["Key"] + "=" + dict["Value"]));

                //// The EventId isn't used at all in the DiagnosticSource API
                //WriteLog($"{args.Payload[0]}: {args.Payload[1]}:{arguments}");
            }
            else
            {
                EventWritten?.Invoke(this, JsonConvert.SerializeObject(args, Formatting.Indented));
            }
        }
    }
}
