using Microsoft.Extensions.Caching.Memory;
using Shortly.Application.DTOs;
using Shortly.Application.Interfaces;
using Shortly.Domain.Entities;

namespace Shortly.Application.Services;

public sealed class LinkService : ILinkService
{
    private readonly ILogger<LinkService> _logger;
    private readonly ILinkRepository _linkRepository;
    private readonly IMemoryCache _cache;

    public LinkService(ILinkRepository linkRepository, ILogger<LinkService> logger, IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _linkRepository = linkRepository ?? throw new ArgumentNullException(nameof(linkRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<LinkResponse> CreateLink(string url, long userId)
    {
        _logger.LogDebug("Creating link for URL: {Url} and userId: {UserId}", url, userId);

        var shortUrl = Ulid.NewUlid().ToString()[..12].ToLowerInvariant();
        var link = new Link(url, shortUrl, userId);

        await _linkRepository.AddAsync(link);
        await _linkRepository.SaveChangesAsync();

        _logger.LogInformation("Link created successfully with shortUrl: {ShortUrl} and id: {Id}.", link.ShortUrl, link.Id);
        return LinkResponse.From(link);
    }

    public async Task<LinkResponse> IncrementClicks(long linkId)
    {
        _logger.LogDebug("Incrementing clicks for linkId: {LinkId}", linkId);

        var link = await _linkRepository.GetByIdAsync(linkId);
        if (link is null)
        {
            _logger.LogWarning("IncrementClicks failed: No link found with id {LinkId}.", linkId);
            throw new KeyNotFoundException($"No link found with id '{linkId}'.");
        }

        link.IncrementClicks();
        await _linkRepository.SaveChangesAsync();

        _logger.LogInformation("Clicks incremented for linkId: {LinkId}. Total clicks: {Clicks}.", link.Id, link.Clicks);
        return LinkResponse.From(link);
    }

    public async Task<LinkResponse> GetLink(string shortUrl)
    {
        _logger.LogDebug("Retrieving link with shortUrl: {ShortUrl}.", shortUrl);
        _logger.LogDebug("Attempting to retrieve link with cache key.");
        var cacheKey = $"link:{shortUrl}";

        if (_cache.TryGetValue(cacheKey, out Link? cached))
        {
            if (cached != null)
            {
                _logger.LogInformation("Link retrieved successfully from cache with shortUrl: {ShortUrl} and id: {Id}.", cached.Hash, cached.Id);
                return LinkResponse.From(cached);
            }
        }

        _logger.LogDebug("Attempting to retrieve link from database.");
        var link = await _linkRepository.GetByShortUrlAsync(shortUrl);
        if (link is null)
        {
            _logger.LogWarning("Link not found with shortUrl {ShortUrl}.", shortUrl);
            throw new KeyNotFoundException($"No link found with shortUrl '{shortUrl}'.");
        }
        _logger.LogDebug("Setting cache key spanning for 30 minutes.");
        _cache.Set(
            cacheKey,
            link,
            TimeSpan.FromMinutes(30));
        _logger.LogInformation("Link retrieved successfully with shortUrl: {ShortUrl} and id: {Id}.", link.Hash, link.Id);
        return LinkResponse.From(link);
    }

    public async Task<List<LinkResponse>> GetAllLinks()
    {
        _logger.LogDebug("Retrieving all links from the database ..");
        var links = await _linkRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} links from the database.", links.Count);
        return links.Select(LinkResponse.From).ToList();
    }

    public async Task<List<LinkResponse>> GetLinksByUserId(long userId)
    {
        _logger.LogDebug("Retrieving links for userId: {UserId}", userId);
        var links = await _linkRepository.GetByUserIdAsync(userId);

        _logger.LogInformation("Retrieved {Count} links for userId: {UserId}.", links.Count, userId);
        return links.Select(LinkResponse.From).ToList();
    }

    public async Task<bool> DeleteLink(string shortUrl)
    {
        _logger.LogDebug("Deleting link: {shorturl}",shortUrl);
        var link = await _linkRepository.GetByShortUrlAsync(shortUrl);

        if(link==null)
        {
            _logger.LogWarning("Link not found: {shortUrl}.", shortUrl);
            return false;
        }

        await _linkRepository.DeleteAsync(link);
        await _linkRepository.SaveChangesAsync();

        _cache.Remove($"link:{shortUrl}");

        _logger.LogInformation("Successfully deleted link: {shortUrl}.",shortUrl);
        return true;
    }
    
}
