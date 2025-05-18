using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    // לא בשימוש, לשימוש עתידי
    public class Tag
    {
        [Key]
        public int TagID { get; set; }
        public string TagName { get; set; }

        public ICollection<ClothingItemTag> ClothingItemTags { get; set; }
    }
}
