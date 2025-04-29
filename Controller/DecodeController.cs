using Microsoft.AspNetCore.Mvc;
using DecodeAI.Web.Services;
using DecodeAI.Web.Models;
using System.IO;
using System.Threading.Tasks;

namespace DecodeAI.Web.Controllers
{
    public class DecodeController : Controller
    {
        private readonly DecodeService _decodeService;
        private readonly EncodeService _encodeService;

        public DecodeController()
        {
            _decodeService = new DecodeService();
            _encodeService = new EncodeService();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return View("Index");

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var (decodedText, isBinary) = await _decodeService.DecodeFileAsync(filePath);

            var model = new DecodeResult
            {
                FileName = file.FileName,
                DecodedText = decodedText,
                IsBinary = isBinary
            };

            return View("Edit", model);
        }

        [HttpPost]
        public async Task<IActionResult> EncodeFile(string fileName, string editedText)
        {
            if (string.IsNullOrEmpty(fileName))
                return RedirectToAction("Index");

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var outputsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "outputs");

            if (!Directory.Exists(outputsPath))
                Directory.CreateDirectory(outputsPath);

            var outputFilePath = Path.Combine(outputsPath, fileName);

            if (editedText == "Bu dosya binary dosyadır. İçeriği düzenlenemez.")
            {
                var uploadFilePath = Path.Combine(uploadsPath, fileName);
                await _encodeService.EncodeBinaryFileAsync(uploadFilePath, outputFilePath);
            }
            else
            {
                await _encodeService.EncodeTextToFileAsync(editedText, outputFilePath);
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
            return File(bytes, "application/octet-stream", fileName);
        }
    }
}
