using System.Text.RegularExpressions;
using Conductor.Client;
using Conductor.Client.Authentication;
using Conductor.Client.Worker;
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

            var host = WorkflowTaskHost.CreateWorkerHost(configuration, new ExtractContent());
            var extractTask = host.StartAsync();

            var host1 = WorkflowTaskHost.CreateWorkerHost(configuration, new CompareContent());
            var compareTask = host1.StartAsync();

            var host2 = WorkflowTaskHost.CreateWorkerHost(configuration, new UploadContent());
            var uploadTask = host2.StartAsync();

            var host3 = WorkflowTaskHost.CreateWorkerHost(configuration, new ExtractParameters());
            var parametersTask = host3.StartAsync();

            await parametersTask;
            await extractTask;
            await compareTask;
            await uploadTask;

            Thread.Sleep(TimeSpan.FromSeconds(100));
        }
    }
}