using System.Collections.Concurrent;
using System.Diagnostics;

namespace DiagnosticSourceTracing
{
    /// <summary>
    /// Simple listener that turns <see cref="TraceSource"/> calls into <see cref="DiagnosticSource"/> 
    /// events.
    /// </summary>
    class DiagnosticSourceTraceListener : TraceListener
    {
        ConcurrentDictionary<string, DiagnosticSource> sources = new ConcurrentDictionary<string, DiagnosticSource>();

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            base.TraceData(eventCache, source, eventType, id, data);

            var tracer = sources.GetOrAdd(source, name => new DiagnosticListener(name));
            var evnt = id.ToString();
            // Simply map event id as "name" and type and data as additional context, say.
            if (tracer.IsEnabled(evnt, new { EventType = eventType, Data = data }))
            {
                tracer.Write(evnt, new { EventType = eventType, Data = data });
            }
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            base.TraceData(eventCache, source, eventType, id, data);

            var tracer = sources.GetOrAdd(source, name => new DiagnosticListener(name));
            var evnt = id.ToString();
            // Simply map event id as "name" and type and data as additional context, say.
            if (tracer.IsEnabled(evnt, new { EventType = eventType, Data = data }))
            {
                tracer.Write(evnt, new { EventType = eventType, Data = data });
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            base.TraceEvent(eventCache, source, eventType, id);

            var tracer = sources.GetOrAdd(source, name => new DiagnosticListener(name));
            var evnt = id.ToString();
            // Simply map event id as "name" and type as additional context, say.
            if (tracer.IsEnabled(evnt, new { EventType = eventType }))
            {
                tracer.Write(evnt, new { EventType = eventType });
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            base.TraceEvent(eventCache, source, eventType, id, format, args);

            var tracer = sources.GetOrAdd(source, name => new DiagnosticListener(name));
            var evnt = id.ToString();
            // Simply map event id as "name" and type and format as additional context, say.
            if (tracer.IsEnabled(evnt, new { EventType = eventType, Format = format, Args = args }))
            {
                tracer.Write(evnt, new { EventType = eventType, Message = string.Format(format, args), Format = format, Args = args });
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            base.TraceEvent(eventCache, source, eventType, id, message);

            var tracer = sources.GetOrAdd(source, name => new DiagnosticListener(name));
            var evnt = id.ToString();
            // Simply map event id as "name" and type and format as additional context, say.
            if (tracer.IsEnabled(evnt, new { EventType = eventType, Message = message }))
            {
                tracer.Write(evnt, new { EventType = eventType, Message = message });
            }
        }

        public override void Write(string message) { }

        public override void WriteLine(string message) { }
    }
}
