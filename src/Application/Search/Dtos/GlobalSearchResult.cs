namespace Application.Search.Dtos;


public class GlobalSearchResult
{
  
    public string Kind { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Subtitle { get; set; }

    public string Href { get; set; } = default!;
}
