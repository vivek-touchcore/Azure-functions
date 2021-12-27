using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace Azure_Functions_TEST
{
    public static class DurableFunction
    {
        [FunctionName("DurableFunction")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            var blobLists = await context.CallActivityAsync<List<string>>("DurableFunctionGetBlobs", null);
            await context.CallActivityAsync<Task>("DurableFunctionGetBase64", blobLists);
            return outputs;
        }

        [FunctionName("DurableFunctionGetBlobs")]
        public static async Task<List<string>> GetBlobs([ActivityTrigger] ILogger log)
        {
            var blobLists = new List<string>();

            BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("ConnectionString"));

            var blobContainer = blobServiceClient.GetBlobContainerClient("images");
            await foreach (BlobItem blob in blobContainer.GetBlobsAsync()) 
            {
                blobLists.Add(blob.Name);
                
            }
            
            return blobLists;
        }

        [FunctionName("DurableFunctionGetBase64")]
        public static async Task GetBase64([ActivityTrigger] List<string> blobItems, ILogger log)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("ConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("images");
            

            foreach(var item in blobItems)
            {
                Console.WriteLine(item);
                var blobBlockRef = blobContainer.GetBlockBlobReference(item);
                using var blobStream = new MemoryStream();
                await blobBlockRef.DownloadToStreamAsync(blobStream).ConfigureAwait(false);
                Console.WriteLine(Convert.ToBase64String(blobStream.ToArray()));
            }
        }

        [FunctionName("DurableFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}