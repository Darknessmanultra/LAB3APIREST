using Shortly.Application.Interfaces;

public sealed class GetLinkHandler
{
    private readonly ILinkReadRepository _repository;

    public GetLinkHandler(
        ILinkReadRepository repository)
    {
        _repository = repository;
    }

    public Task<LinkReadModel?> HandleAsync(GetLinkQuery query)
    {
        return _repository.GetAsync(query.ShortUrl);
    }
}