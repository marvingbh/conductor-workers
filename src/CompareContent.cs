using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using Conductor.Client.Worker;
using Newtonsoft.Json;
using Workers.Dtos;

namespace Workers;

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

    public static List<Folder> CompareFolders(Folder[] first, Folder[] second)
    {
        var result = new List<Folder>();

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
                    var newFolder = new Folder
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