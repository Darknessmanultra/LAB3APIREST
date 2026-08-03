public sealed class UpdateLinkHandler
{
    private readonly ILinkWriteRepository _repository;

    public UpdateLinkHandler(ILinkWriteRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(UpdateLinkCommand command)
    {
        var link = await _repository.GetByIdAsync(command.ShortUrl);

        if (link is null)
            throw new KeyNotFoundException();

        link.IncrementClicks();

        await _repository.SaveChangesAsync();
    }
}