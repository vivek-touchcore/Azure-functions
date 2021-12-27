using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Azure_Functions_TEST
{
    public static class Function
    {
        [FunctionName("Upload")]
        public static async Task<IActionResult> UploadImage(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequest req,
            ILogger log)
        {
            var file = req.Form.Files["File"];
            string filePath = $"content/images/{file.FileName}";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("connectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainerInstance = blobClient.GetContainerReference("master");

            await blobContainerInstance.CreateIfNotExistsAsync().ConfigureAwait(false);
            var blob = blobContainerInstance.GetBlockBlobReference(filePath);

            blob.Properties.ContentType = file.ContentType;
            await blob.UploadFromStreamAsync(file.OpenReadStream()).ConfigureAwait(false);



            ResponseDTO response = new ResponseDTO() 
            { 
                URL = blob.Uri.AbsoluteUri
            };

            return new OkObjectResult(response);
        }

        [FunctionName("Download")]
        public static async Task<IActionResult> DownloadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "download")] HttpRequest req, ILogger log)
        {

            string streamData = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(streamData);
            string URL = data?.filePath;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("connectionString"));
            
            // var blobClient = storageAccount.CreateCloudBlobClient();
            // var blobContainer = blobClient.GetContainerReference("images");
            // var blob = blobContainer.GetBlobBlobReference(filepath);
            
            var blob = new CloudBlockBlob(new Uri(URL), storageAccount.Credentials);
            string base64String;
            using var blobStream = new MemoryStream();
            await blob.DownloadToStreamAsync(blobStream).ConfigureAwait(false);
            base64String = Convert.ToBase64String(blobStream.ToArray());
            string contentType = blob.Properties.ContentType;
            string fileName = blob.Name.Split("/")[^1];
            
            ResponseDTO response = new ResponseDTO()
            {
                Content = base64String,
                ContentType = contentType,
                FileName = fileName
            };
           
            string cloudQueueResponse = JsonConvert.SerializeObject(response);


            //Storage Queue
            CloudQueueClient cloudQueueclient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = cloudQueueclient.GetQueueReference("upload");
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            CloudQueueMessage queueMessage = new CloudQueueMessage(cloudQueueResponse);
            await queue.AddMessageAsync(queueMessage).ConfigureAwait(false);


            //Service bus Queue
            //var client = new QueueClient(Environment.GetEnvironmentVariable("AzureWebJobsServiceBus"), "images-service-bus");
            //var message = new Message(Encoding.UTF8.GetBytes(cloudQueueResponse));
            //await client.SendAsync(message);


            //Service bus Topic 
            //var client = new ServiceBusClient(Environment.GetEnvironmentVariable("AzureWebJobsServiceBus"));
            //var sender = client.CreateSender("uploads");
            //using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
            //messageBatch.TryAddMessage(new ServiceBusMessage(cloudQueueResponse));
            //await sender.SendMessagesAsync(messageBatch);

            //Service bus Topic
            //var clients = new TopicClient(Environment.GetEnvironmentVariable("AzureWebJobsServiceBus"), "uploads");
            //await clients.SendAsync(new Message(Encoding.UTF8.GetBytes(cloudQueueResponse))).ConfigureAwait(false);

            return new OkObjectResult(response);
        }

        [FunctionName("QueueTrigger")]
        [return: Queue("upload", Connection = "AzureWebJobsStorage")]
        public static string Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue")] HttpRequest req, ILogger log) 
        {
            
            return "random queue string";
        }

        [FunctionName("ServiceBusTrigger")]
        [return: ServiceBus("images-service-bus")]
        public static string ServiceBusTrigger([HttpTrigger(AuthorizationLevel.Function, "get", Route = "servicebus")] HttpRequest req, ILogger log)
        {
            return "random service bus string";
        }

    }

    public class ResponseDTO
    {
        public string URL { get; set; }

        public string Content { get; set; }

        public string ContentType { get; set; }

        public string FileName { get; set; }
    }
}
