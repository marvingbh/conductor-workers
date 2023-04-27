using Conductor.Client.Interfaces;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workers
{
    internal class WorkflowsWorkerService : BackgroundService
    {
        private readonly IWorkflowTaskCoordinator workflowTaskCoordinator;
        private readonly IEnumerable<IWorkflowTask> workflowTasks;

        public WorkflowsWorkerService(
            IWorkflowTaskCoordinator workflowTaskCoordinator,
            IEnumerable<IWorkflowTask> workflowTasks
        )
        {
            this.workflowTaskCoordinator = workflowTaskCoordinator;
            this.workflowTasks = workflowTasks;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var worker in workflowTasks)
            {
                workflowTaskCoordinator.RegisterWorker(worker);
            }
            // start all the workers so that it can poll for the tasks
            await workflowTaskCoordinator.Start();
        }
    }
}
