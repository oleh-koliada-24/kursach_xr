using Microsoft.AspNetCore.Mvc;
using Api.Services;

namespace Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class AnonymizationController : ControllerBase
    {
        private readonly IFaceAnonymizationService _faceAnonymizationService;

        public AnonymizationController(IFaceAnonymizationService faceAnonymizationService)
        {
            _faceAnonymizationService = faceAnonymizationService;
        }

        [HttpPost("anonymization")]
        public async Task<IActionResult> AnonymizeImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image file provided");
            }

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
            {
                return BadRequest("Invalid file type. Only JPEG, PNG and GIF are allowed.");
            }

            if (image.Length > 10 * 1024 * 1024)
            {
                return BadRequest("File size exceeds 10MB limit");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();

                // Використовуємо сервіс для анонімізації облич
                byte[] anonymizedImageBytes = await _faceAnonymizationService.AnonymizeFacesAsync(imageBytes);

                return File(anonymizedImageBytes, "image/jpeg");
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error while processing image");
            }
        }
    }
}
