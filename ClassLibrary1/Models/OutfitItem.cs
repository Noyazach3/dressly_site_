using System;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    public class OutfitItem
    {
        [Key]
        public int OutfitItemID { get; set; }
        public int OutfitID { get; set; }
        public int ItemID { get; set; }

        public Outfit Outfit { get; set; }
        public ClothingItem ClothingItem { get; set; }
    }
}
