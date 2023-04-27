using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Conductor.Client;
using Conductor.Client.Authentication;
using Conductor.Client.Extensions;
using Conductor.Client.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Workers
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            var configuration = new Configuration()
            {
                BasePath = "http://localhost:8080/api/"

            };

            await new HostBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    // First argument is optional headers which client wasnt to pass.
                    services.AddConductorWorker(configuration);
                    services.AddConductorWorkflowTask<ExtractContent>();
                    services.AddConductorWorkflowTask<CompareContent>();
                    services.AddConductorWorkflowTask<UploadContent>();
                    services.AddConductorWorkflowTask<ExtractParameters>();
                    services.AddHostedService<WorkflowsWorkerService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConsole();
                })
                .RunConsoleAsync();
            Console.ReadLine();

        }
    }
}