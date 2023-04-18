using System.Text.RegularExpressions;
using APIClient;
using Conductor.Client;
using Conductor.Client.Authentication;
using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using Conductor.Client.Worker;
using Newtonsoft.Json;
using Workers.Application;
using Workers.Dtos;
using Task = System.Threading.Tasks.Task;

namespace Workers
{
    internal class Program
    {
        static async Task Main(string[] args)
        {



            //var teamId = "6419dca0e9c2050042416825";
            //var client = new FlorenceApi("https://us.uat.api.researchbinders.com/ebinder/v1", "https://auth.uat.api.researchbinders.com");

            //await client.AuthenticateAsync("W4B5eVkg4BOCdMRdzNJrrPQjeKEUpJis", "u6Urn4HWqlJBWzhjFhsHVsBw2SzQNE49r5SrouKqHgoDkXSLnJ3nA_taEBqLfRuy", "https://uat.api.researchbinders.com");


            //var contentServ = new ContentServices(client);

            //var (contentFile, name) = await contentServ.DownloadContent("6419dca0e9c2050042416825", "643f0427e859ac00413647d1");

            //var response = await contentServ.UploadContent("6419dca0a5223c00499b9496", contentFile, name,
            //    "6435cf087ef91a00430687e6", "643f044247ce9c004314160e");

            //Console.WriteLine(response);


            //Console.WriteLine(response);
            //var folderServices = new FolderServices(client);

            //var folder = await folderServices.GetFoldersAndContents(teamId);

            //Call a GET API endpoint with query string parameters

            //Console.WriteLine(folder.Length);


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

    public class ExtractContent : IWorkflowTask
    {
        public string TaskType { get; }
        public WorkflowTaskExecutorConfiguration WorkerSettings { get; }
        string workerId = Guid.NewGuid().ToString();
        public ExtractContent(string taskType = "extract_content_task")
        {
            TaskType = taskType;
            WorkerSettings = new WorkflowTaskExecutorConfiguration();
        }

        public TaskResult Execute(Conductor.Client.Models.Task task)
        {
            var input = task.InputData;
            var teamId = input["destinationId"].ToString();
            var client = new FlorenceApi("https://us.uat.api.researchbinders.com/ebinder/v1", "https://auth.uat.api.researchbinders.com");

            // Authenticate with the API
            client.AuthenticateAsync("W4B5eVkg4BOCdMRdzNJrrPQjeKEUpJis", "u6Urn4HWqlJBWzhjFhsHVsBw2SzQNE49r5SrouKqHgoDkXSLnJ3nA_taEBqLfRuy", "https://uat.api.researchbinders.com").GetAwaiter().GetResult();

            var folderServices = new FolderServices(client);

            var folder = folderServices.GetFoldersAndContents(teamId).GetAwaiter().GetResult();
            var output = new Dictionary<string, object>
            {
                { "contents", folder ?? new Folders[]{} },
            };

            return task.Completed(output);
        }
    }

    public class CompareContent : IWorkflowTask
    {
        public string TaskType { get; }
        public WorkflowTaskExecutorConfiguration WorkerSettings { get; }
        string workerId = Guid.NewGuid().ToString();
        public CompareContent(string taskType = "compare_content_task")
        {
            TaskType = taskType;
            WorkerSettings = new WorkflowTaskExecutorConfiguration();
        }

        public TaskResult Execute(Conductor.Client.Models.Task task)
        {
            var input = task.InputData;
            var sourceContent = JsonConvert.DeserializeObject<InputContents>(input["sourcecontent"].ToString());
            var destinationContent = JsonConvert.DeserializeObject<InputContents>(input["destinationcontent"].ToString());

            var result = CompareFolders(sourceContent?.contents, destinationContent?.contents);

            var output = new Dictionary<string, object>
            {
                { "files", result },
            };
            return task.Completed(output);
        }

        public static List<Folders> CompareFolders(Folders[] first, Folders[] second)
        {
            var result = new List<Folders>();

            foreach (var folder in first)
            {
                var match = second.FirstOrDefault(sf => sf.name == folder.name);
                if (match == null)
                {
                    folder.SetShouldCreateFolder(true);
                    if (folder.Contents.Any())
                    {
                        result.Add(folder);
                    }
                }
                else
                {
                    folder.id = match.id;
                    folder.SetShouldCreateFolder(false);
                    var newContents = folder.Contents
                        .Where(c => !match.Contents.Any(sc => sc.title == c.title && sc.subType == c.subType))
                        .ToList();

                    if (newContents.Any())
                    {
                        var newFolder = new Folders
                        {
                            id = folder.id,
                            name = folder.name,
                            Contents = newContents
                        };
                        result.Add(newFolder);
                    }
                }
            }

            return result;
        }
    }

    public class UploadContent : IWorkflowTask
    {
        public string TaskType { get; }
        public WorkflowTaskExecutorConfiguration WorkerSettings { get; }
        string workerId = Guid.NewGuid().ToString();
        public UploadContent(string taskType = "upload_content_task")
        {
            TaskType = taskType;
            WorkerSettings = new WorkflowTaskExecutorConfiguration();
        }

        public TaskResult Execute(Conductor.Client.Models.Task task)
        {
            try
            {
                var input = task.InputData;
                string teamId = input["sourceId"].ToString()!;
                string destinationId = input["destinationId"].ToString()!;
                string destinationBinderId = input["destinationBinderId"].ToString()!;

                var client = new FlorenceApi("https://us.uat.api.researchbinders.com/ebinder/v1", "https://auth.uat.api.researchbinders.com");
                var uploadContent = JsonConvert.DeserializeObject<UploadContentDto>(input["uploadcontent"].ToString());

                // Authenticate with the API
                client.AuthenticateAsync("W4B5eVkg4BOCdMRdzNJrrPQjeKEUpJis", "u6Urn4HWqlJBWzhjFhsHVsBw2SzQNE49r5SrouKqHgoDkXSLnJ3nA_taEBqLfRuy", "https://uat.api.researchbinders.com").GetAwaiter().GetResult();
                //Download Content
                var contentService = new ContentServices(client);
                foreach (var folder in uploadContent.files)
                {
                    if (folder.ShouldCreateFolder)
                    {
                        //create folder
                        continue;
                    }
                    foreach (var content in folder.Contents)
                    {
                        var (fileBytes, fileName) = contentService.DownloadContent(teamId, content.id).GetAwaiter().GetResult();

                        var response = contentService.UploadContent(destinationId, fileBytes, fileName+".pdf",
                            destinationBinderId, folder.id).GetAwaiter().GetResult();
                    }
                }

                return task.Completed();
            }
            catch (Exception e)
            {
                return task.Failed(e.Message);
            }
        }
    }



    public class ExtractParameters : IWorkflowTask
    {
        public string TaskType { get; }
        public WorkflowTaskExecutorConfiguration WorkerSettings { get; }
        string workerId = Guid.NewGuid().ToString();

        public ExtractParameters(string taskType = "extract_parameters_task")
        {
            TaskType = taskType;
            WorkerSettings = new WorkflowTaskExecutorConfiguration();
        }

        public TaskResult Execute(Conductor.Client.Models.Task task)
        {
            var input = task.InputData;
            var destinations = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(input["destinations"].ToString());
            var sourceContent = input["sourceContent"].ToString();
            var sourceId = input["source"].ToString();


            var dynamicTasks = new List<object>();
            var dynamicTasksInput = new Dictionary<string, Dictionary<string, string>>();

            foreach (var destination in destinations)
            {
                var destId = destination["id"];
                var destinationBinderId = destination["binderId"];
                var taskRefName = $"destination_{destId}";

                dynamicTasks.Add(new {
                    subWorkflowParam = new { name = "ReplicationToDestination" }, taskReferenceName = taskRefName, type = "SUB_WORKFLOW",
                    
            });

                dynamicTasksInput[taskRefName] = new Dictionary<string, string>
                {
                    { "sourceContent", sourceContent ?? "" },
                    { "destinationId", destId },
                    { "sourceId", sourceId ?? ""},
                    { "destinationBinderId", destinationBinderId }
                };
            }

            var output = new Dictionary<string, object>
            {
                { "dynamicTasks", dynamicTasks },
                { "dynamicTasksInput", dynamicTasksInput }
            };

            return task.Completed(output);
        }
    }

}