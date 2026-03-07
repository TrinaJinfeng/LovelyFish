using Microsoft.Extensions.Options;
using LovelyFish.API.Server.Models;

namespace LovelyFish.API.Server.Services
{
    public class BlobService
    {
        private readonly string _uploadDirectory;
        private readonly string _apiBaseUrl;

        public BlobService(IOptions<BlobSettings> options, IOptions<EmailSettings> emailOptions)
        {
            _uploadDirectory = options.Value.UploadDirectory;
            _apiBaseUrl = emailOptions.Value.ApiBaseUrl;

            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            //var extension = Path.GetExtension(fileName);
            //var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadDirectory, fileName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            return fileName;
        }

        public async Task DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_uploadDirectory, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            await Task.CompletedTask;
        }
    }
}

