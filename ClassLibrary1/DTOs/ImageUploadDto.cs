using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.DTOs
{
    public class ImageUploadDto
    {
        [Required]
        public int ItemID { get; set; }

        [Required]
        public IFormFile ImageFile { get; set; }
    }
}
