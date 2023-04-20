using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using Conductor.Client.Worker;
using Newtonsoft.Json;

namespace Workers;

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