using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure_Functions_TEST
{
    public static class ServiceBus
    {
        [FunctionName("ServiceBus")]
        public static void Run([ServiceBusTrigger("images-service-bus", Connection = "AzureWebJobsServiceBus")]string myQueueItem, ILogger log)
        {
            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            log.LogWarning($"ServiceBus queue trigger Function processed image: {data.FileName}");
        }
    }
}
