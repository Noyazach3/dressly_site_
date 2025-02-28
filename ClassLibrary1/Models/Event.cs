using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassLibrary1.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        public int UserID { get; set; }
        public string EventName { get; set; }
        public DateTime? EventDate { get; set; }

        public User User { get; set; }
        public ICollection<Outfit> Outfits { get; set; }
    }
}
