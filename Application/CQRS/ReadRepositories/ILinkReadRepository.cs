public interface ILinkReadRepository
{
    Task<LinkReadModel?> GetAsync(string ShortUrl);
    Task<List<LinkReadModel?>> GetbyUserIdAsync(long? UserId);

    Task<List<LinkReadModel?>>GetAllAsync();
}