using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.DTOs;

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
        public async Task<IActionResult> AnonymizeImage(AnonymizationDTO dto)
        {
            var image = dto.Image;

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

                var sessionId = dto.SessionId ?? Guid.NewGuid().ToString();
                byte[] anonymizedImageBytes = _faceAnonymizationService.AnonymizeFaces(imageBytes, dto.Type, sessionId);

                var contentType = image.ContentType.ToLower().Contains("gif") ? "image/gif" : "image/jpeg";
                return File(anonymizedImageBytes, contentType);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error while processing image");
            }
        }
    }
}
