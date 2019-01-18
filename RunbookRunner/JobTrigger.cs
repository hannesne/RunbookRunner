using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace RunbookRunner
{
    public static class JobTrigger
    {
        

        [FunctionName("JobTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            ILogger log)
        {
            // Check if an instance with the specified ID already exists.
            var existingInstance = await orchestrationClient.GetStatusAsync(JobProcessor.SingletonInstanceId);
            if (existingInstance == null || existingInstance.RuntimeStatus != OrchestrationRuntimeStatus.Running)
            {
                // An instance with the specified ID doesn't exist, create one.
                await orchestrationClient.StartNewAsync(nameof(JobProcessor), JobProcessor.SingletonInstanceId, new [] { Guid.NewGuid().ToString() });
                log.LogDebug("Requested processor start");
            }
            else
            {
                await orchestrationClient.RaiseEventAsync(JobProcessor.SingletonInstanceId, JobProcessor.JobQueuedEventName, Guid.NewGuid());
                log.LogDebug("Request processor queued");
            }
            return new OkResult();

        }
    }
}
