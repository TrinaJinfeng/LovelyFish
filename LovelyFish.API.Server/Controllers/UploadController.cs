// ProductUploadController.cs
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
        }

        [HttpPost]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("没有文件上传");

            var uploadedFiles = new List<object>();
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // 只返回文件名，数据库存这个
                    uploadedFiles.Add(new { fileName });
                }
            }

            return Ok(uploadedFiles);
        }

        [HttpDelete("delete/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", fileName);
            if (System.IO.File.Exists(uploadPath))
            {
                System.IO.File.Delete(uploadPath);
                return Ok("删除成功");
            }
            return NotFound("文件不存在");
        }
    }
}


//上传返回 fileName，数据库存它即可。

//删除接口可以直接传 fileName 删除服务器文件。
