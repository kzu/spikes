using System;
using System.Diagnostics.Tracing;

namespace TracingApp
{
    partial class Program
    {
        public class EventSourceInfo
        {
            public string Name { get; set; }
            public Guid Guid { get; set; }
            public EventSourceSettings Settings { get; set; }
        }
    }
}
