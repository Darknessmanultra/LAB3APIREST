using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shortly.Infrastructure.Persistence;

public sealed class LinkReadRepository : ILinkReadRepository
{
    private readonly AppDbContext _context;

    public LinkReadRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<LinkReadModel?> GetAsync(string ShortUrl)
    {
        return _context.LinkReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShortUrl == ShortUrl);
    }

    public Task<List<LinkReadModel?>> GetbyUserIdAsync(long? UserId)
    {
        return _context.LinkReadModels
            .AsNoTracking().Where(l => l.UserId == UserId).ToListAsync();
    }

    public Task<List<LinkReadModel?>> GetAllAsync()
    {
        return _context.LinkReadModels.AsNoTracking().ToListAsync();
    }
}