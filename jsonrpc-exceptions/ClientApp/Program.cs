using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using StreamJsonRpc;

namespace ClientApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var process = Process.Start(new ProcessStartInfo("ServerApp.exe")
            {
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });

            var server = JsonRpc.Attach<IService>(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);

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

            Console.WriteLine("Enter to exit client and server.");
            Console.ReadLine();
            await server.StopAsync();
        }
    }
}
