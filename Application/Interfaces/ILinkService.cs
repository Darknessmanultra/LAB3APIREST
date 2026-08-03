using Shortly.Application.DTOs;

namespace Shortly.Application.Interfaces;

public interface ILinkService
{
    Task<LinkResponse> CreateLink(CreateLinkCommand command);

    Task<LinkResponse> IncrementClicks(UpdateLinkCommand command);

    Task<LinkResponse> GetLink(GetLinkQuery query);

    Task<List<LinkResponse>> GetAllLinks();

    Task<List<LinkResponse>> GetLinksByUserId(GetLinkQuery query);

    Task<bool> DeleteLink(DeleteLinkCommand);
}
