using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationWorkerClient
    {
        private readonly TaskHubClient taskHubClient;

        public OrchestrationWorkerClient(IOrchestrationServiceClient orchestrationServiceClient)
        {
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
        }

        public async Task<OrchestrationInstance> JumpStartOrchestrationAsync(Job job)
        {
            try
            {
                return await this.taskHubClient.CreateOrchestrationInstanceAsync(
                    job.Orchestration.Name,
                    job.Orchestration.Version,
                    job.InstanceId,
                    job.Input);
            }
            catch (Exception ex)
            {
                OrchestrationEventSource.Log.TraceEvent(TraceEventType.Critical, "OrchestrationWorker", string.Format("Orchestration Start Failed: Id-{0},Message-{1}", job.InstanceId, ex.Message), ex.ToString(), "Error");
                return null;
            }
        }

        public async Task<OrchestrationState> WaitForOrchestrationAsync(OrchestrationInstance instance, TimeSpan timeout)
        {
            return await this.taskHubClient.WaitForOrchestrationAsync(instance, timeout);
        }

        public async Task<OrchestrationState> WaitForOrchestrationAsync(OrchestrationInstance instance, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return await this.taskHubClient.WaitForOrchestrationAsync(instance, timeout, cancellationToken);
        }

        public async Task<OrchestrationState> GetOrchestrationStateAsync(string instanceId)
        {
            return await this.taskHubClient.GetOrchestrationStateAsync(instanceId);
        }

        //
        // Summary:
        //     Get a list of orchestration states from the instance storage table for either
        //     the most current or all executions (generations) of the specified instance.
        //
        // Parameters:
        //   instanceId:
        //     Instance id
        //
        //   allExecutions:
        //     True if method should fetch all executions of the instance, false if the method
        //     should only fetch the most recent execution
        //
        // Returns:
        //     List of OrchestrationState objects that represents the list of orchestrations
        //     in the instance store
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Thrown if instance store not configured
        public async Task<IList<OrchestrationState>> GetOrchestrationStateAsync(string instanceId, bool allExecutions)
        {
            return await this.taskHubClient.GetOrchestrationStateAsync(instanceId, allExecutions);
        }

        //
        // Summary:
        //     Get a list of orchestration states from the instance storage table for the most
        //     current execution (generation) of the specified instance.
        //
        // Parameters:
        //   instance:
        //     Instance
        //
        // Returns:
        //     The OrchestrationState of the specified instanceId or null if not found
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Thrown if instance store not configured
        public async Task<OrchestrationState> GetOrchestrationStateAsync(OrchestrationInstance instance)
        {
            return await this.taskHubClient.GetOrchestrationStateAsync(instance);
        }

        //
        // Summary:
        //     Get a list of orchestration states from the instance storage table for the specified
        //     execution (generation) of the specified instance.
        //
        // Parameters:
        //   instanceId:
        //     Instance id
        //
        //   executionId:
        //     Execution id
        //
        // Returns:
        //     The OrchestrationState of the specified instanceId or null if not found
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Thrown if instance store not configured
        public async Task<OrchestrationState> GetOrchestrationStateAsync(string instanceId, string executionId)
        {
            return await this.taskHubClient.GetOrchestrationStateAsync(instanceId, executionId);
        }

        //
        // Summary:
        //     Get a string dump of the execution history of the specified orchestration instance
        //     specified execution (generation) of the specified instance.
        //
        // Parameters:
        //   instance:
        //     Instance
        //
        // Returns:
        //     String with formatted JSON representing the execution history
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Thrown if instance store not configured
        public async Task<string> GetOrchestrationHistoryAsync(OrchestrationInstance instance)
        {
            return await this.taskHubClient.GetOrchestrationHistoryAsync(instance);
        }

        //
        // Summary:
        //     Purges orchestration instance state and history for orchestrations older than
        //     the specified threshold time.
        //
        // Parameters:
        //   thresholdDateTimeUtc:
        //     Threshold date time in UTC
        //
        //   timeRangeFilterType:
        //     What to compare the threshold date time against
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Thrown if instance store not configured
        public Task PurgeOrchestrationInstanceHistoryAsync(DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            return this.taskHubClient.PurgeOrchestrationInstanceHistoryAsync(thresholdDateTimeUtc, timeRangeFilterType);
        }

        //
        // Summary:
        //     Raises an event in the specified orchestration instance, which eventually causes
        //     the OnEvent() method in the orchestration to fire.
        //
        // Parameters:
        //   orchestrationInstance:
        //     Instance in which to raise the event
        //
        //   eventName:
        //     Name of the event
        //
        //   eventData:
        //     Data for the event
        public Task RaiseEventAsync(OrchestrationInstance orchestrationInstance, string eventName, object eventData)
        {
            return this.taskHubClient.RaiseEventAsync(orchestrationInstance, eventName, eventData);
        }

        //
        // Summary:
        //     Forcefully terminate the specified orchestration instance
        //
        // Parameters:
        //   orchestrationInstance:
        //     Instance to terminate
        public Task TerminateInstanceAsync(OrchestrationInstance orchestrationInstance)
        {
            return this.taskHubClient.TerminateInstanceAsync(orchestrationInstance);
        }

        //
        // Summary:
        //     Forcefully terminate the specified orchestration instance with a reason
        //
        // Parameters:
        //   orchestrationInstance:
        //     Instance to terminate
        //
        //   reason:
        //     Reason for terminating the instance

        public Task TerminateInstanceAsync(OrchestrationInstance orchestrationInstance, string reason)
        {
            return this.taskHubClient.TerminateInstanceAsync(orchestrationInstance, reason);
        }
    }
}