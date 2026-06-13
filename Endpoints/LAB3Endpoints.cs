using Shortly.Application.Interfaces;

namespace Shortly.Endpoints;

public static class Lab3Endpoints
{
    public static IEndpointRouteBuilder MapLab3Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/urls");

        group.MapPost("/", CreateLink);

        group.MapGet("/", GetAllLinks);

        group.MapGet("/{id}", GetLink);

        group.MapDelete("/{id}", DeleteLink);

        group.MapGet("/{id}/stats", GetStats);

        return app;
    }

    private static async Task<IResult> CreateLink(LinkRequest request, ILinkService service)
    {
        var link = await service.CreateLink(request.Url,request.Id);

        return Results.Created($"/api/links/{link.Id}",link);
    }

    private static async Task<IResult> GetAllLinks(ILinkService service)
    {
        return Results.Ok(await service.GetAllLinks());
    }

    private static async Task<IResult> GetLink(string shortUrl,ILinkService service)
    {
        var link = await service.GetLink(shortUrl);

        return link is null
            ? Results.NotFound()
            : Results.Ok(link);
    }

    private static async Task<IResult> DeleteLink(string shortUrl,ILinkService service)
    {
        var deleted = await service.DeleteLink(shortUrl);

        return deleted
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> GetStats(
        string id,
        ILinkService service)
    {
        var link = await service.GetLink(id);

        return link is null
            ? Results.NotFound()
            : Results.Ok(new
            {
                link.Id,
                link.Clicks,
            });
    }
}