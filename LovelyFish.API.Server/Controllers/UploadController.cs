using Microsoft.AspNetCore.Mvc;
using LovelyFish.API.Server.Services;

namespace LovelyFish.API.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController] // Enables API-specific behaviors like automatic model binding and 400 responses
    public class UploadController : ControllerBase
    {
        private readonly BlobService _blobService;

        // Inject BlobService to handle file storage
        public UploadController(BlobService blobService)
        {
            _blobService = blobService;
        }

        // ==================== Upload Files ====================
        // POST api/upload
        [HttpPost]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {
            // Validate input
            if (files == null || files.Count == 0)
            {
                Console.WriteLine("[UploadFiles] No files uploaded");
                return BadRequest("No files uploaded");
            }

            var uploadedFiles = new List<object>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // Generate unique file name to avoid collisions
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                    try
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            // Upload file to blob storage
                            var fileUrl = await _blobService.UploadFileAsync(stream, fileName);
                            uploadedFiles.Add(new { fileName, fileUrl });
                        }
                        Console.WriteLine("[UploadFiles] Upload succeeded: " + fileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[UploadFiles] Upload failed: " + ex.Message);
                    }
                }
            }

            // Return list of uploaded files with URLs
            return Ok(uploadedFiles);
        }

        // ==================== Delete File ====================
        // DELETE api/upload/delete/{fileName}
        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                // Delete file from blob storage
                await _blobService.DeleteFileAsync(fileName);
                Console.WriteLine("[DeleteFile] Deleted successfully: " + fileName);
                return Ok("File deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DeleteFile] Delete error: " + ex.Message);
                return StatusCode(500, "File deletion failed");
            }
        }
    }
}
