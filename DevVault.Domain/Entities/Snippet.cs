
namespace DevVault.Domain.Entities;

public class Snippet
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public string Language { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private Snippet() { }

    public static Snippet Create(string title, string content, string language, Guid userId) 
    {
        if (string.IsNullOrWhiteSpace(title)) 
         throw new ArgumentException("Title cannot be empty");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty");

        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be empty");
        return new Snippet
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Language = language,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };
 
    }

}
