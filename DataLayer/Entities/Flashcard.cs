namespace DataLayer.Entities;

public class Flashcard : BaseGuidEntity
{
    public Guid FlashcardSetId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public int Order { get; set; }

    public FlashcardSet FlashcardSet { get; set; } = null!;
}
