using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    // לא בשימוש, לשימוש עתידי
    public class Color
    {
        [Key]
        public int ColorID { get; set; }
        public string ColorName { get; set; }
        public string ComplementaryColor { get; set; }
        public string ContrastColor { get; set; }
        public short IsNeutral { get; set; }

        public ICollection<ClothingItem> ClothingItems { get; set; }
    }
}
