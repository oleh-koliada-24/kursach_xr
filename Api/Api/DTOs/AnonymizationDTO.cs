namespace Api.DTOs
{
    public enum AnonymizationType
    {
        Blur,
        Pixelate,
        Blackout
    }

    public class AnonymizationDTO
    {
        public IFormFile? Image { get; set; }
        public AnonymizationType Type { get; set; }
        public string? SessionId { get; set; }
    }
}
