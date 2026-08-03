public sealed class DeleteLinkHandler
{
    private readonly ILinkWriteRepository _repository;

    public DeleteLinkHandler(ILinkWriteRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(DeleteLinkCommand command)
    {
        var link = await _repository.GetByIdAsync(command.ShortUrl);

        if (link is null)
            throw new KeyNotFoundException();

        await _repository.DeleteAsync(link);

        await _repository.SaveChangesAsync();
    }
}