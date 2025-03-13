namespace ClassLibrary1.Models
{
    public class Image
    {
        public int ImageID { get; set; }
        public int OwnerID { get; set; } // מקושר לפריט לבוש
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string? ImageType { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}
