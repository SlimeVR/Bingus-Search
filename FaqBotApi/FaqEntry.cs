namespace FaqBatApi;

public record FaqEntryResponse
{
    public float Relevance { get; set; }

    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}
