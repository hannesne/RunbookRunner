using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
            Task<string> timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddMinutes(5),jobId, CancellationToken.None);
            
            //todo: add a timer to timeout our wait for the job to complete.
            while (true)
            {
                
                Task<string> queuedEvent = context.WaitForExternalEvent<string>(JobQueuedEventName);    
                Task<string> completedTask = await Task.WhenAny(jobCompletedEvent, queuedEvent, timeoutTask);
                if (completedTask == queuedEvent)
                {
                    string queuedJobId = completedTask.Result;
                    log.LogDebug($"Job queued {queuedJobId}");
                    queuedJobs.Enqueue(queuedJobId);
                }
                else
                {
                    //todo: check that job id matches the one we're waiting for to complete.

                    if (completedTask == timeoutTask)
                    {
                        //todo: do some logging or retry logic here.
                        log.LogDebug($"Job timed out {jobId}");
                        
                    }
                    else
                    { 
                        log.LogDebug($"Job completed {jobId}");
                    }

                    if (queuedJobs.Count > 0)
                    {
                        context.ContinueAsNew(queuedJobs); //This is required to keep the execution history from growing indefinitely.
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