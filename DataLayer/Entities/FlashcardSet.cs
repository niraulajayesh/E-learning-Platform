namespace DataLayer.Entities;

public class FlashcardSet : BaseGuidEntity
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;

    public Category Category { get; set; } = null!;
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
    public ICollection<Flashcard> Cards
    {
        get => Flashcards;
        set => Flashcards = value;
    }
}
