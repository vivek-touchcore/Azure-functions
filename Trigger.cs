using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace Azure_Functions_TEST
{
    public static class Trigger
    {
        [FunctionName("Blobtrigger")]
        public static async Task Run([BlobTrigger("master/content/images/{name}", Connection = "connectionString")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            string filePath = $"processed/images/{name}";

            string extension = name.Split(".")[name.Split(".").Length - 1];

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("connectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainerInstance = blobClient.GetContainerReference("images");
            await blobContainerInstance.CreateIfNotExistsAsync().ConfigureAwait(false);
            var blob = blobContainerInstance.GetBlockBlobReference(filePath);

            await blob.UploadFromStreamAsync(myBlob);

            log.LogInformation("File processed successfully");

        }
    }
}
