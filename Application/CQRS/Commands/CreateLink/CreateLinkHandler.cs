using Shortly.Application.DTOs;
using Shortly.Application.Interfaces;
using Shortly.Domain.Entities;

public sealed class CreateLinkHandler
{
    private readonly ILinkRepository _repository;

    public CreateLinkHandler(ILinkRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(CreateLinkCommand command)
    {
        if (!Uri.TryCreate(
            command.OriginalUrl,
            UriKind.Absolute,
            out _))
        {
            throw new ArgumentException("Invalid URL.");
        }

        var ulid = Ulid.NewUlid().ToString()[..12].ToLowerInvariant();
        var link = new Link(command.OriginalUrl, ulid, command.userId);

        await _repository.AddAsync(link);

        await _repository.SaveChangesAsync();

    }
}