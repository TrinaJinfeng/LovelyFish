using Microsoft.AspNetCore.Mvc;

namespace LovelyFish.API.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
            Console.WriteLine("[UploadController] WebRootPath = " + _env.WebRootPath);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {

            if (files == null || files.Count == 0)
            {
                Console.WriteLine("[UploadFiles] 没有文件上传");
                return BadRequest("没有文件上传");
            }

            var uploadedFiles = new List<object>();
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            try
            {
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadPath, fileName);

                        try
                        {
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            uploadedFiles.Add(new { fileName });
                            Console.WriteLine("[UploadFiles] 保存成功: " + fileName);
                        }
                        catch (Exception exFile)
                        {
                            Console.WriteLine("[UploadFiles] 保存失败: " + exFile.Message);
                        }
                    }
                }

                return Ok(uploadedFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UploadFiles] 上传异常: " + ex.Message);
                return StatusCode(500, "上传失败");
            }
        }

        [HttpDelete("delete/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads", fileName);

                if (System.IO.File.Exists(uploadPath))
                {
                    System.IO.File.Delete(uploadPath);
                    Console.WriteLine("[DeleteFile] 删除成功: " + fileName);
                    return Ok("删除成功");
                }
                else
                {
                    Console.WriteLine("[DeleteFile] 文件不存在: " + fileName);
                    return NotFound("文件不存在");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DeleteFile] 删除异常: " + ex.Message);
                return StatusCode(500, "删除失败");
            }
        }
    }
}
