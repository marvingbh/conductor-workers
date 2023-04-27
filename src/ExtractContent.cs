using APIClient;

using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using Conductor.Client.Worker;

using RestSharp;
using System;
using Workers.Application;
using Workers.Dtos;

namespace Workers;

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
        var teamId = input["teamId"].ToString();
        var binderId = input["binderId"].ToString();
        var client = new FlorenceApi("https://us.uat.api.researchbinders.com/ebinder/v1", "https://auth.uat.api.researchbinders.com");

        // Authenticate with the API
        client.AuthenticateAsync("W4B5eVkg4BOCdMRdzNJrrPQjeKEUpJis", "u6Urn4HWqlJBWzhjFhsHVsBw2SzQNE49r5SrouKqHgoDkXSLnJ3nA_taEBqLfRuy",
            "https://uat.api.researchbinders.com").GetAwaiter().GetResult();

        var folderServices = new FolderServices(client);

        var folder = folderServices.BuildFolderTreeAsync(teamId).GetAwaiter().GetResult();


        var output = new Dictionary<string, object>
        {
            { "contents", folder },
        };

        return task.Completed(output);
    }


}