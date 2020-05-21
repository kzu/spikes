using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using StreamJsonRpc;

namespace ServerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var service = new Service(cts);
            var rpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput(), service);

            while (!cts.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }
    }

    class Service : IService
    {
        CancellationTokenSource cts;

        public Service(CancellationTokenSource cts) => this.cts = cts;

        public Task ExecuteAsync() => throw new ServiceException("Failed", Guid.NewGuid(), ServiceKind.Infrastructure)
        {
            Data =
            {
                { "Caller", nameof(ExecuteAsync) }
            }
        };

        public Task StopAsync()
        {
            cts.Cancel();
            return Task.CompletedTask;
        }
    }
}
