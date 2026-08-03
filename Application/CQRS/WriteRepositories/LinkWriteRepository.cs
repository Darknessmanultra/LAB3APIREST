using Shortly.Domain.Entities;
using Shortly.Infrastructure.Persistence;

public sealed class LinkWriteRepository
    : ILinkWriteRepository
{
    private readonly AppDbContext _context;

    public LinkWriteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Link link)
    {
        await _context.Links.AddAsync(link);
    }

    public Task UpdateAsync(Link link)
    {
        _context.Links.Update(link);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Link link)
    {
        _context.Links.Remove(link);
        return Task.CompletedTask;
    }

    public Task<Link?> GetByIdAsync(String id)
    {
        return _context.Links.FindAsync(id).AsTask();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}