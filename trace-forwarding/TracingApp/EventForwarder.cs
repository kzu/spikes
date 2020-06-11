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

        readonly HashSet<string> enabledSources = new HashSet<string>();
        readonly List<DiagnosticListener> allListeners;
        readonly EventListener listener;

        public EventForwarder(IProgress<string> receiver)
        {
            allListeners = new List<DiagnosticListener>();
            DiagnosticListener.AllListeners.Subscribe(d => allListeners.Add(d));

            listener = new EventListener();
            // Just serialize and pass-through to calling domain
            // TODO: see if we can leverage https://github.com/dotnet/runtime/blob/master/docs/design/features/raw-eventlistener.md somehow.
            listener.EventWritten += (sender, args) => receiver.Report(JsonConvert.SerializeObject(args));
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
    }
}
