# Custom exception serialization in StreamJsonRpc

In order to make the programming model more intuitive in RPC scenarios, 
this spike modifies [StreamJsonRpc](https://github.com/microsoft/vs-streamjsonrpc/) 
[exceptions handling](https://github.com/microsoft/vs-streamjsonrpc/blob/master/doc/exceptions.md) 
with a hacked-up tweak that serializes the exception using the [ISerializable](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.iserializable?view=netcore-3.1) 
support already built-in .NET exceptions. 

The server can simply throw its custom exceptions, including custom [Data](https://docs.microsoft.com/en-us/dotnet/api/system.exception.data?view=netcore-3.1):

```csharp
throw new ServiceException("Failed", Guid.NewGuid(), ServiceKind.Infrastructure)
{
    Data =
    {
        { "Caller", nameof(ExecuteAsync) }
    }
};
```

And the client can catch that via the `InnerException` thrown by the RPC client:

```csharp
try
{
    await server.ExecuteAsync();
}
catch (Exception ex) when (ex.InnerException is ServiceException se && se.ServiceKind == ServiceKind.Infrastructure)
{
    // NOTE: client can access the actual inner exception, also get typed custom data
    // as well as the generic exception Data bag too.
    Console.WriteLine($"{se.Message}: {se.EventId} ({se.ServiceKind}, from {se.Data["Caller"]})");
}
```

The `ServiceException` would be a type shared between both sides through the same assembly that 
provides the service contract itself.