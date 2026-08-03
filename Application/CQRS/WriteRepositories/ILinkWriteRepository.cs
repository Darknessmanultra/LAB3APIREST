using Shortly.Domain.Entities;

public interface ILinkWriteRepository
{
    Task AddAsync(Link link);

    Task UpdateAsync(Link link);

    Task DeleteAsync(Link link);

    Task<Link?> GetByIdAsync(String id);

    Task SaveChangesAsync();
}