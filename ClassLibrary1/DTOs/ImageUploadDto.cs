using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.DTOs
{
    public class ImageUploadDto
    {
        public int ItemID { get; set; }
        public IFormFile ImageFile { get; set; }
    }

}
