using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace RunbookRunner
{
    public static class JobProcessor
    {
        public const string SingletonInstanceId = "JobProcessor";
        public const string JobQueuedEventName = "JobQueued";
        public const string JobCompletedEventName = "JobCompleted";
        

        [FunctionName("JobProcessor")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            log.LogDebug("Processor starting");

            Queue<string> queuedJobs = context.GetInput<Queue<string>>();
            string jobId = queuedJobs.Dequeue();
            

            await context.CallActivityAsync("JobProcessor_StartJob", jobId);
            Task<string> jobCompletedEvent = context.WaitForExternalEvent<string>(JobCompletedEventName);
            //todo: add a timer to timeout our wait for the job to complete.
            while (true)
            {
                
                Task<string> JobQueuedEvent = context.WaitForExternalEvent<string>(JobQueuedEventName);
                Task<string> completedTask = await Task.WhenAny(new[] { jobCompletedEvent, JobQueuedEvent });

                if (completedTask != jobCompletedEvent)
                {
                    string queuedJobId = completedTask.Result;
                    log.LogDebug($"Job queued {queuedJobId}");
                    queuedJobs.Enqueue(queuedJobId);
                }
                else
                {
                    //todo: check that job id matches the one we're waiting for to complete.
                    log.LogDebug($"Job completed {jobId}");
                    if (queuedJobs.Count > 0)
                    {
                        context.ContinueAsNew(queuedJobs); //This is required to keep the execution history from growing indefintely.
                    }
                    break;
                }
            }

        }

        [FunctionName("JobProcessor_StartJob")]
        public static string StartJob([ActivityTrigger] string jobId, ILogger log)
        {
            log.LogInformation($"Initialising Job {jobId}");
            return jobId;
        }

        
    }
}