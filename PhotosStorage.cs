using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Storage.Blob;
using PhotosAzure.Models;
using Photos.AnalyzerService.Abstractions;

namespace PhotosAzure
{
    public  class PhotosStorage
    {
        public readonly IAnalyzerService analyzerService;

        public PhotosStorage(IAnalyzerService analyzerService)
        {
            this.analyzerService = analyzerService;
        }


        [FunctionName("PhotosStorage")]
        public  async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,  "post", Route = null)] HttpRequest req,
            [Blob("photos", FileAccess.ReadWrite, Connection = Literals.StorageConnectionString )] CloudBlobContainer blobContainer,
            [CosmosDB("photos", "metadata", ConnectionStringSetting = Literals.CosmosDBConnection, CreateIfNotExists = true)] IAsyncCollector<dynamic> items,
            ILogger logger)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<PhotoUploadModel>(body);

            var newId = Guid.NewGuid();
            var blobName = $"{newId}.jpg";

            await blobContainer.CreateIfNotExistsAsync();

           try
            {
                var cloudBlockBlob = blobContainer.GetBlockBlobReference(blobName);

                var photoBytes = Convert.FromBase64String(request.Photo);
                await cloudBlockBlob.UploadFromByteArrayAsync(photoBytes, 0, photoBytes.Length);

                var analysisResult = await analyzerService.AnalyzeAsync(photoBytes);

                var item = new
                {
                    id = newId,
                    name = request.Name,
                    description = request.Description,
                    tags = request.Tags,
                    analysis = analysisResult
                };

                await items.AddAsync(item);

                logger?.LogInformation($"Successfully uploaded {newId}.jpg file and its metadata");

                return new OkObjectResult(newId);
            }
            catch (Exception ex)
            {

                logger?.LogInformation($"Ocurrio un problema  {ex.Message}");
            }

            return new BadRequestResult();

        }
    }
}
