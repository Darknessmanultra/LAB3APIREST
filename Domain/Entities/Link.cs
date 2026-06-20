using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Shortly.Domain.Entities;

[Table("links")]
[Index(nameof(ShortUrl), IsUnique = true)]
public class Link
{
    [Key]
    public long Id { get; private set; }

    [Required]
    [MaxLength(20248)]
    public string Url { get; private set; } = null!;

    [Required]
    [MaxLength(32)]
    public string ShortUrl { get; private set; } = null!;

    [Required]
    public string Hash {get;private set;} = null!;

    public DateTime UpdatedAt {get;set;}

    [Required] public int Clicks { get; private set; }

    [ForeignKey(nameof(User))]
    public long UserId { get; private set; }

    public User User { get; private set; } = null!;

    private Link()
    {
    }

    public Link(string url, string shortUrl, long userId)
    {
        Url = string.IsNullOrWhiteSpace(url)
            ? throw new ArgumentException("URL is required.", nameof(url))
            : url.Trim();

        ShortUrl = string.IsNullOrWhiteSpace(shortUrl)
            ? throw new ArgumentException("ShortUrl is required.", nameof(shortUrl))
            : shortUrl.Trim();

        UserId = userId > 0
            ? userId
            : throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be greater than zero.");

        Clicks = 0;
        Hash = GenerateShortCode(ShortUrl);
        UpdatedAt=DateTime.UtcNow;
    }

    
    private static string GenerateShortCode(string ShortUrl)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(ShortUrl));

        return Convert.ToHexString(bytes)[..8];
    }

    public void IncrementClicks() => Clicks++;

}
