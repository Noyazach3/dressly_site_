using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ClassLibrary1.Models
{
    public class Outfit
    {
        public int OutfitID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public DateTime? DateCreated { get; set; }
        public int EventID { get; set; }

        public User User { get; set; }
        public Event Event { get; set; }
        public ICollection<OutfitItem> OutfitItems { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
    }
}
