using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    public class ClothingItemTag
    {
        [Key]
        public int ItemTagID { get; set; }
        public int ItemID { get; set; }
        public int TagID { get; set; }

        public ClothingItem ClothingItem { get; set; }
        public Tag Tag { get; set; }
    }
}
