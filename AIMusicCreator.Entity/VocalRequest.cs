using Microsoft.AspNetCore.Http;

namespace AIMusicCreator.Entity
{
    public class VocalRequest
    {
        public IFormFile MelodyMidi { get; set; } = null!;
        public string Lyrics { get; set; } = string.Empty;
        public string Language { get; set; } = "zh";
    }

}
