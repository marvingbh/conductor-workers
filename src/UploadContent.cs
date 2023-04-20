using APIClient;
using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using Conductor.Client.Worker;
using Newtonsoft.Json;
using Workers.Application;
using Workers.Dtos;

namespace Workers;

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