namespace ClassLibrary1.DTOs
{
    // DTO – Data Transfer Object:
    // מחלקה זו משמשת להעברת מידע מתוך עמוד תכנון אאוטפיט לפי נושא.
    // היא כוללת את העונה, הסגנון, ואת הפריטים שנבחרו על ידי המשתמש.
    public class OutfitSelection
    {
        public string Season { get; set; } = "Summer";
        public int Style { get; set; } = 1;
        public Dictionary<string, ClassLibrary1.Models.ClothingItem> SelectedItems { get; set; } = new();
        public bool IsOutfitComplete { get; set; } = false;

    }
}
