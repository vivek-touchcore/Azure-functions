using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure_Functions_TEST
{
    public static class Queue
    {
        [FunctionName("Queue")]
        public static void Run([QueueTrigger("upload", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed");
            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            log.LogWarning($"File download completed : {data.FileName}");
        }
    }
}
