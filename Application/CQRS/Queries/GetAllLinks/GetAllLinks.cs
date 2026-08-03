public sealed class GetAllLinks
{
    private readonly ILinkReadRepository _repository;

    public GetAllLinks(ILinkReadRepository repository)
    {
        _repository = repository;
    }

    public Task<List<LinkReadModel?>> HandleAsync()
    {
        return _repository.GetAllAsync();
    }
}