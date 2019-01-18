using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace RunbookRunner
{
    public static class JobUpdater
    {

        [FunctionName("JobUpdater")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            ILogger log)
        {
            //todo: get jobId
            string jobId = "";

            await orchestrationClient.RaiseEventAsync(JobProcessor.SingletonInstanceId, JobProcessor.JobCompletedEventName, jobId);
            return new OkResult();
        }
    }
}
