namespace ClassLibrary1.DTOs
{
    public class OutfitSaveDto
    {
        public int? UserID { get; set; }
        public string Name { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<int> ClothingItemIDs { get; set; } = new();
    }
}
