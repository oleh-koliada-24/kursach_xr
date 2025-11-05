using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class AnonymizationController : ControllerBase
    {
        public AnonymizationController()
        {

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

                byte[] anonymizedImageBytes = await AnonymizeImageBytes(imageBytes);

                return File(anonymizedImageBytes, image.ContentType);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error while processing image");
            }
        }

        private async Task<byte[]> AnonymizeImageBytes(byte[] imageBytes)
        {
            await Task.Delay(1000);
            return imageBytes;
        }
    }
}
