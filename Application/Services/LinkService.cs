using Microsoft.Extensions.Caching.Memory;
using Shortly.Application.DTOs;
using Shortly.Application.Interfaces;
using Shortly.Domain.Entities;

namespace Shortly.Application.Services;

public sealed class LinkService : ILinkService
{
    private readonly ILogger<LinkService> _logger;
    private readonly ILinkReadRepository _linkReadRepository;
    private readonly ILinkWriteRepository _linkWriteRepository;
    private readonly IMemoryCache _cache;

    public LinkService(ILinkReadRepository linkReadRepository, ILinkWriteRepository linkWriteRepository, ILogger<LinkService> logger, IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _linkReadRepository = linkReadRepository ?? throw new ArgumentNullException(nameof(linkReadRepository));
        _linkWriteRepository = linkWriteRepository ?? throw new ArgumentNullException(nameof(linkWriteRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<LinkResponse> CreateLink(CreateLinkCommand command)
    {
        _logger.LogDebug("Creating link for URL: {command.OriginalUrl} and userId: {Command.UserId}", command.OriginalUrl, command.userId);

        var shortUrl = Ulid.NewUlid().ToString()[..12].ToLowerInvariant();
        var link = new Link(command.OriginalUrl, shortUrl, command.userId);

        await _linkWriteRepository.AddAsync(link);
        await _linkWriteRepository.SaveChangesAsync();

        _logger.LogInformation("Link created successfully with shortUrl: {ShortUrl} and id: {Id}.", link.ShortUrl, link.Id);
        return LinkResponse.From(link);
    }

    public async Task<LinkResponse> IncrementClicks(UpdateLinkCommand command)
    {
        _logger.LogDebug("Incrementing clicks for linkId: {command.ShortUrl}", command.ShortUrl);

        var link = await _linkWriteRepository.GetByIdAsync(command.ShortUrl);
        if (link is null)
        {
            _logger.LogWarning("IncrementClicks failed: No link found with id {command.ShortUrl}.", command.ShortUrl);
            throw new KeyNotFoundException($"No link found with id '{command.ShortUrl}'.");
        }

        link.IncrementClicks();
        await _linkWriteRepository.SaveChangesAsync();

        _logger.LogInformation("Clicks incremented for linkId: {command.ShortUrl}. Total clicks: {Clicks}.", link.Id, link.Clicks);
        return LinkResponse.From(link);
    }

    public async Task<LinkResponse> GetLink(GetLinkQuery command)
    {
        _logger.LogDebug("Retrieving link with shortUrl: {command.ShortUrl}.", command.ShortUrl);
        _logger.LogDebug("Attempting to retrieve link with cache key.");
        var cacheKey = $"link:{command.ShortUrl}";

        if (_cache.TryGetValue(cacheKey, out Link? cached))
        {
            if (cached != null)
            {
                _logger.LogInformation("Link retrieved successfully from cache with shortUrl: {ShortUrl} and id: {Id}.", cached.Hash, cached.Id);
                return LinkResponse.From(cached);
            }
        }

        _logger.LogDebug("Attempting to retrieve link from database.");
        var link = await _linkReadRepository.GetAsync(command.ShortUrl);
        if (link is null)
        {
            _logger.LogWarning("Link not found with shortUrl {ShortUrl}.", command.ShortUrl);
            throw new KeyNotFoundException($"No link found with shortUrl '{command.ShortUrl}'.");
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
        var links = await _linkReadRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} links from the database.", links.Count);
        return links.Select(LinkResponse.From).ToList();
    }

    public async Task<List<LinkReadModel>> GetLinksByUserId(GetLinkQuery query)
    {
        _logger.LogDebug("Retrieving links for userId: {query.UserId}", query.UserId);
        var links = await _linkReadRepository.GetbyUserIdAsync(query.UserId);

        //_logger.LogInformation("Retrieved {TotalClicks} links for userId: {query.UserId}.", links.TotalClicks, query.UserId);
        return links;
    }

    public async Task<bool> DeleteLink(DeleteLinkCommand command)
    {
        _logger.LogDebug("Deleting link: {command.ShortUrl}",command.ShortUrl);
        var link = await _linkWriteRepository.GetByIdAsync(command.ShortUrl);

        if(link==null)
        {
            _logger.LogWarning("Link not found: {shortUrl}.", command.ShortUrl);
            return false;
        }

        await _linkWriteRepository.DeleteAsync(link);
        await _linkWriteRepository.SaveChangesAsync();

        _cache.Remove($"link:{command.ShortUrl}");

        _logger.LogInformation("Successfully deleted link: {command.ShortUrl}.",command.ShortUrl);
        return true;
    }
    
}
