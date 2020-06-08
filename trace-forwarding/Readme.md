# Capturing Traces

Showcases how a (mostly) single approach can be used to collect traces from all three main 
logging APIs in .NET: [TraceSource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracesource?view=netcore-3.1), 
[DiagnosticSource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource?view=netcore-3.1) and 
[EventSource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource?view=netcore-3.1), using 
[EventListener](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventlistener?view=netcore-3.1).

[![capturing traces](CapturingTraces.gif)](CapturingTraces.mp4)

The [Component.cs](TracingApp/Component.cs) showcases a simple scenario (a "clock" ticking 
every second) that traces using all three APIs. The console app allows enabling/disabling 
the capturing of those traces by entering `[+|-]TracingApp-[DiagnosticSourced|EventSourced|TraceSourced]` 
to enable/disable capturing of each of the three. A temp log file is opened via `code` 
on startup to visualize the effect without polluting the console output.