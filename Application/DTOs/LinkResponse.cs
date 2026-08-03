using Shortly.Domain.Entities;

namespace Shortly.Application.DTOs;

public class LinkResponse
{
    public long Id { get; init; }
    public string Url { get; init; } = null!;
    public string ShortUrl { get; init; } = null!;
    public int Clicks { get; init; }
    public DateTime UpdatedAt {get;init;}
    public static LinkResponse From(Link link) => new()
    {
        Id = link.Id,
        Url = link.Url,
        ShortUrl = link.Hash,
        Clicks = link.Clicks,
        UpdatedAt = link.UpdatedAt
    };

    public static LinkResponse From(LinkReadModel link) => new()
    {
        Id = link.UserId,
        Url = link.OriginalUrl,
        ShortUrl = link.Hash,
        Clicks = link.TotalClicks,
        UpdatedAt = link.LastAccessed
    };
}
