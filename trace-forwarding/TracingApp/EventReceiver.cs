using System;

namespace TracingApp
{
    class EventReceiver : MarshalByRefObject, IProgress<string>
    {
        readonly Action<string> report;

        public EventReceiver(Action<string> report) => this.report = report;

        public void Report(string value) => report(value);
    }
}
