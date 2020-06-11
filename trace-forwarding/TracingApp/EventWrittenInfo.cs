using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TracingApp
{
    partial class Program
    {
        class EventWrittenInfo
        {
            public string EventName { get; set; }
            public int EventId { get; set; }
            public EventKeywords Keywords { get; set; }
            public EventChannel Channel { get; set; }
            public EventLevel Level { get; set; }
            public string Message { get; set; }
            public EventOpcode Opcode { get; set; }
            [JsonProperty(ItemConverterType = typeof(PayloadConverter))]
            public List<object> Payload { get; set; }
            public List<string> PayloadNames { get; set; }
            public EventTags Tags { get; set; }
            public EventTask Task { get; set; }
            public byte Version { get; set; }

            public EventSourceInfo EventSource { get; set; }
        }
    }
}
