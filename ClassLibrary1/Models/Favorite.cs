using System;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    public class Favorite
    {
        [Key]
        public int FavoriteID { get; set; }
        public int UserID { get; set; }
        public int OutfitID { get; set; }
        public int ItemID { get; set; }

        public User User { get; set; }
        public Outfit Outfit { get; set; }
        public ClothingItem ClothingItem { get; set; }
    }
}
