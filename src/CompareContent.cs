using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using Conductor.Client.Worker;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
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
        var sourceContent = JsonConvert.DeserializeObject<InputContents>(input["sourcecontent"].ToString() ?? string.Empty);
        var destinationContent = JsonConvert.DeserializeObject<InputContents>(input["destinationcontent"].ToString() ?? string.Empty);

        var result = CompareFolders(sourceContent?.contents, destinationContent?.contents);

        var output = new Dictionary<string, object>
        {
            { "files", result },
        };
        return task.Completed(output);
    }

    public static List<Folder> CompareFolders(Folder[] sourceFolders, Folder[] destinationFolders)
    {
        var result = new List<Folder>();

        foreach (var folder in sourceFolders)
        {
            var match = destinationFolders.FirstOrDefault(sf => sf.name == folder.name);
            if (match == null)
            {
                folder.SetShouldCreateFolder(true);
                SetShouldCreateFolderRecursivy(true, folder.SubFolders);
                result.Add(folder);
            }
            else
            {
                folder.id = match.id;
                folder.SetShouldCreateFolder(false);

                var comparedSubfolders = CompareFolders(folder.SubFolders.ToArray(), match.SubFolders.ToArray());
                folder.SubFolders = comparedSubfolders;

                var newContents = folder.Contents
                    .Where(c => !match.Contents.Any(sc => sc.title == c.title && sc.subType == c.subType))
                    .ToList();

                if (newContents.Any() || comparedSubfolders.Any())
                {
                    var newFolder = new Folder
                    {
                        id = match.id,
                        name = folder.name,
                        Contents = newContents,
                        SubFolders = comparedSubfolders
                    };
                    result.Add(newFolder);
                }
            }
        }

        return result;
    }

    public static void SetShouldCreateFolderRecursivy(bool shouldCreateFolder, List<Folder> folders)
    {
        foreach (var folder in folders)
        {
            folder.SetShouldCreateFolder(shouldCreateFolder);
            if (folder.SubFolders.Any())
            {
                SetShouldCreateFolderRecursivy(shouldCreateFolder, folder.SubFolders);
            }
        }

    }

}