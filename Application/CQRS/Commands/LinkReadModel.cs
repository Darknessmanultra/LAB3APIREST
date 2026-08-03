using System.Security.Cryptography;
using System.Text;

public sealed class LinkReadModel
{
    public string ShortUrl { get; set; } = string.Empty;

    public string OriginalUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public long UserId { get; private set; }

    public int TotalClicks { get; set; }

    public DateTime LastAccessed { get; set; }
    public string Hash {get;private set;} = null!;
    
    private LinkReadModel()
    {
    }

    public LinkReadModel(string url, string shortUrl, long userId)
    {
        OriginalUrl = string.IsNullOrWhiteSpace(url)
            ? throw new ArgumentException("URL is required.", nameof(url))
            : url.Trim();

        ShortUrl = string.IsNullOrWhiteSpace(shortUrl)
            ? throw new ArgumentException("ShortUrl is required.", nameof(shortUrl))
            : shortUrl.Trim();

        UserId = userId > 0
            ? userId
            : throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be greater than zero.");

        TotalClicks = 0;
        Hash = GenerateShortCode(ShortUrl);
        LastAccessed=DateTime.UtcNow;
    }

    
    private static string GenerateShortCode(string ShortUrl)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(ShortUrl));

        return Convert.ToHexString(bytes)[..8];
    }
}