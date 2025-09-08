using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using LovelyFish.API.Server.Models;

namespace LovelyFish.API.Server.Services
{
    public class BlobService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobService(IOptions<BlobSettings> options)
        {
            var blobSettings = options.Value;
            var blobServiceClient = new BlobServiceClient(blobSettings.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(blobSettings.ContainerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, true);
            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}

